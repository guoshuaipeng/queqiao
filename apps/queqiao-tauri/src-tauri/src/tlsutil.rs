//! 业务 TLS。指纹是证书 DER 的 SHA-256，与 .NET `TlsCertUtil.Sha256Fingerprint` 对齐。

use std::sync::Arc;

use rcgen::{CertificateParams, DnType, ExtendedKeyUsagePurpose, IsCa, KeyPair, KeyUsagePurpose};
use rustls::client::danger::{HandshakeSignatureValid, ServerCertVerified, ServerCertVerifier};
use rustls::crypto::{verify_tls12_signature, verify_tls13_signature, CryptoProvider};
use rustls::pki_types::{CertificateDer, PrivateKeyDer, PrivatePkcs8KeyDer, ServerName, UnixTime};
use rustls::{ClientConfig, DigitallySignedStruct, Error, ServerConfig, SignatureScheme};
use sha2::{Digest, Sha256};

pub struct Identity {
    pub cert_der: Vec<u8>,
    pub key_der: Vec<u8>,
    pub fingerprint: String,
}

pub fn fingerprint_of(der: &[u8]) -> String {
    let dig = Sha256::digest(der);
    dig.iter().map(|b| format!("{b:02X}")).collect()
}

pub fn create_identity() -> Result<Identity, String> {
    let key = KeyPair::generate().map_err(|_| "没能准备连接。".to_string())?;
    let mut params = CertificateParams::new(vec!["RemoteAssist.Agent".to_string()])
        .map_err(|_| "没能准备连接。".to_string())?;
    params
        .distinguished_name
        .push(DnType::CommonName, "RemoteAssist.Agent");
    params.is_ca = IsCa::NoCa;
    params.key_usages = vec![
        KeyUsagePurpose::DigitalSignature,
        KeyUsagePurpose::KeyEncipherment,
    ];
    params.extended_key_usages = vec![ExtendedKeyUsagePurpose::ServerAuth];
    let cert = params
        .self_signed(&key)
        .map_err(|_| "没能准备连接。".to_string())?;
    let cert_der = cert.der().to_vec();
    let key_der = key.serialize_der();
    let fingerprint = fingerprint_of(&cert_der);
    if fingerprint.len() != 64 {
        return Err("没能准备连接。".into());
    }
    Ok(Identity {
        cert_der,
        key_der,
        fingerprint,
    })
}

pub fn load_or_create_identity(path: &std::path::Path) -> Result<Identity, String> {
    if let Some(id) = read_identity(path) {
        if id.fingerprint.len() == 64 {
            return Ok(id);
        }
    }
    let id = create_identity()?;
    write_identity(path, &id)?;
    Ok(id)
}

fn read_identity(path: &std::path::Path) -> Option<Identity> {
    let text = std::fs::read_to_string(path).ok()?;
    let parsed: serde_json::Value = serde_json::from_str(&text).ok()?;
    let cert = hex_decode(parsed.get("cert")?.as_str()?)?;
    let key = hex_decode(parsed.get("key")?.as_str()?)?;
    if cert.is_empty() || key.is_empty() {
        return None;
    }
    let fingerprint = fingerprint_of(&cert);
    Some(Identity {
        cert_der: cert,
        key_der: key,
        fingerprint,
    })
}

fn write_identity(path: &std::path::Path, identity: &Identity) -> Result<(), String> {
    if let Some(parent) = path.parent() {
        std::fs::create_dir_all(parent).map_err(|_| "没能保存这台电脑的身份。".to_string())?;
    }
    let body = serde_json::json!({
        "cert": hex_encode(&identity.cert_der),
        "key": hex_encode(&identity.key_der),
    });
    std::fs::write(path, body.to_string()).map_err(|_| "没能保存这台电脑的身份。".to_string())
}

fn hex_encode(bytes: &[u8]) -> String {
    bytes.iter().map(|b| format!("{b:02x}")).collect()
}

fn hex_decode(text: &str) -> Option<Vec<u8>> {
    if text.len() % 2 != 0 {
        return None;
    }
    let mut out = Vec::with_capacity(text.len() / 2);
    let bytes = text.as_bytes();
    let mut i = 0;
    while i < bytes.len() {
        let hi = hex_val(bytes[i])?;
        let lo = hex_val(bytes[i + 1])?;
        out.push((hi << 4) | lo);
        i += 2;
    }
    Some(out)
}

fn hex_val(b: u8) -> Option<u8> {
    match b {
        b'0'..=b'9' => Some(b - b'0'),
        b'a'..=b'f' => Some(b - b'a' + 10),
        b'A'..=b'F' => Some(b - b'A' + 10),
        _ => None,
    }
}

fn provider() -> Arc<CryptoProvider> {
    let provider = rustls::crypto::ring::default_provider();
    let _ = provider.install_default();
    Arc::new(rustls::crypto::ring::default_provider())
}

pub fn server_config(identity: &Identity) -> Result<Arc<ServerConfig>, String> {
    let cert = CertificateDer::from(identity.cert_der.clone());
    let key = PrivateKeyDer::Pkcs8(PrivatePkcs8KeyDer::from(identity.key_der.clone()));
    let config = ServerConfig::builder_with_provider(provider())
        .with_protocol_versions(&[&rustls::version::TLS13, &rustls::version::TLS12])
        .map_err(|_| "没能准备加密连接。".to_string())?
        .with_no_client_auth()
        .with_single_cert(vec![cert], key)
        .map_err(|_| "没能准备加密连接。".to_string())?;
    Ok(Arc::new(config))
}

pub fn client_config(expected_fp: &str) -> Result<Arc<ClientConfig>, String> {
    let expected = crate::credential::normalize_fp(expected_fp);
    if !crate::credential::is_sha256_hex(&expected) {
        return Err(crate::copy::INCOMPLETE.into());
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

pub fn server_name() -> ServerName<'static> {
    ServerName::try_from("RemoteAssist.Agent").expect("static name")
}
