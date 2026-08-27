# OAuth 2.0 Device Code Flow

本启动器使用微软 OAuth 2.0 设备代码流实现正版登录。

## Flow Steps

1. 请求设备代码和用户代码 → Azure AD `/devicecode` endpoint
2. 用户在浏览器访问 `microsoft.com/devicelogin` 并输入代码
3. 用户完成微软账户登录
4. 启动器轮询 `/token` endpoint 获取访问令牌
5. 用访问令牌换取 Xbox Live 令牌 (XBL)
6. 用 XBL 令牌换取 XSTS 令牌
7. 用 XSTS 令牌换取 Minecraft 访问令牌
8. 用 Minecraft 令牌获取玩家档案 (UUID + 用户名)

## Key Code Locations

- 认证逻辑: `MinecraftAuthenticator.cs`
- 设备码轮询: `GetAzureTokenByDeviceCode()`
- Xbox 认证: `GetXboxLiveToken()` + `GetXSTSToken()`