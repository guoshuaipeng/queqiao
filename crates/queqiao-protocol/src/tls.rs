//! 业务 TLS 客户端：证书 DER 的 SHA-256 指纹钉扎。

use std::sync::Arc;

use rustls::client::danger::{HandshakeSignatureValid, ServerCertVerified, ServerCertVerifier};
use rustls::crypto::{verify_tls12_signature, verify_tls13_signature, CryptoProvider};
use rustls::pki_types::{CertificateDer, ServerName, UnixTime};
use rustls::{ClientConfig, DigitallySignedStruct, Error, SignatureScheme};
use sha2::{Digest, Sha256};

use crate::credential::{is_sha256_hex, normalize_fp};

pub fn fingerprint_of(der: &[u8]) -> String {
    let dig = Sha256::digest(der);
    dig.iter().map(|b| format!("{b:02X}")).collect()
}

fn provider() -> Arc<CryptoProvider> {
    let provider = rustls::crypto::ring::default_provider();
    let _ = provider.install_default();
    Arc::new(rustls::crypto::ring::default_provider())
}

pub fn client_config(expected_fp: &str) -> Result<Arc<ClientConfig>, String> {
    let expected = normalize_fp(expected_fp);
    if !is_sha256_hex(&expected) {
        return Err("连接凭证不完整，缺少有效证书指纹。".into());
    }
    let prov = provider();
    let verifier = Arc::new(PinVerifier {
        expected,
        algs: prov.signature_verification_algorithms,
    });
    let config = ClientConfig::builder_with_provider(prov)
        .with_protocol_versions(&[&rustls::version::TLS13, &rustls::version::TLS12])
        .map_err(|_| "没能准备加密连接。".to_string())?
        .dangerous()
        .with_custom_certificate_verifier(verifier)
        .with_no_client_auth();
    Ok(Arc::new(config))
}

pub fn server_name() -> ServerName<'static> {
    ServerName::try_from("RemoteAssist.Agent").expect("static name")
}

#[derive(Debug)]
struct PinVerifier {
    expected: String,
    algs: rustls::crypto::WebPkiSupportedAlgorithms,
}

impl ServerCertVerifier for PinVerifier {
    fn verify_server_cert(
        &self,
        end_entity: &CertificateDer<'_>,
        _intermediates: &[CertificateDer<'_>],
        _server_name: &ServerName<'_>,
        _ocsp_response: &[u8],
        _now: UnixTime,
    ) -> Result<ServerCertVerified, Error> {
        let got = fingerprint_of(end_entity.as_ref());
        if got != self.expected {
            return Err(Error::General("pin rejected".into()));
        }
        Ok(ServerCertVerified::assertion())
    }

    fn verify_tls12_signature(
        &self,
        message: &[u8],
        cert: &CertificateDer<'_>,
        dss: &DigitallySignedStruct,
    ) -> Result<HandshakeSignatureValid, Error> {
        verify_tls12_signature(message, cert, dss, &self.algs)
    }

    fn verify_tls13_signature(
        &self,
        message: &[u8],
        cert: &CertificateDer<'_>,
        dss: &DigitallySignedStruct,
    ) -> Result<HandshakeSignatureValid, Error> {
        verify_tls13_signature(message, cert, dss, &self.algs)
    }

    fn supported_verify_schemes(&self) -> Vec<SignatureScheme> {
        self.algs.supported_schemes()
    }
}
