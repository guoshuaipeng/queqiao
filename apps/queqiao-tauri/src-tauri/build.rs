fn main() {
    println!("cargo:rerun-if-changed=../src");
    println!("cargo:rerun-if-changed=../src/index.html");
    println!("cargo:rerun-if-changed=../src/styles.css");
    println!("cargo:rerun-if-changed=../src/main.js");
    tauri_build::build()
}
