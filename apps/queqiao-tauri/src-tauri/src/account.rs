//! 账号用户名和密码规则。注册成功后立刻登录，不在这里保存明文密码。

pub fn username(raw: &str) -> Result<String, &'static str> {
    let name = raw.trim();
    let ok = (3..=32).contains(&name.chars().count())
        && name.chars().all(|c| c.is_ascii_alphanumeric() || c == '_');
    if !ok {
        return Err("用户名要用 3 到 32 位字母、数字或下划线。");
    }
    Ok(name.to_string())
}

pub fn password(raw: &str) -> Result<(), &'static str> {
    if raw.chars().count() < 8 {
        return Err("密码至少 8 位。");
    }
    Ok(())
}

pub fn explain(err: &str) -> String {
    if err.contains("username taken") {
        "这个用户名已经有人用了。".into()
    } else if err.contains("invalid username") {
        "用户名要用 3 到 32 位字母、数字或下划线。".into()
    } else if err.contains("password too short") {
        "密码至少 8 位。".into()
    } else if err.contains("invalid credentials") {
        "用户名或密码不对。".into()
    } else if err.contains("helper already") {
        "这台电脑正在被协助。".into()
    } else if err.contains("peer not online") {
        "这台电脑没开着鹊桥，现在连不上。".into()
    } else if err.contains("device not found") {
        "列表里没有这台电脑。".into()
    } else if err.contains("unauthorized") || err.contains("invalid token") || err.contains("token expired") {
        "登录已失效，请重新登录。".into()
    } else {
        "没能连上，请检查网络后重试。".into()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn username_rules() {
        assert!(username("ab").is_err());
        assert!(username("has space").is_err());
        assert!(username("名字abc").is_err());
        assert_eq!(username("  Alice_1 ").unwrap(), "Alice_1");
    }

    #[test]
    fn password_needs_eight() {
        assert!(password("1234567").is_err());
        assert!(password("12345678").is_ok());
    }
}
