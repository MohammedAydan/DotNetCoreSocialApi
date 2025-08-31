# دليل الإعداد الآمن - Social API

## 🔒 إعداد User Secrets للتطوير

### 1. تفعيل User Secrets
```bash
cd Social
dotnet user-secrets init
```

### 2. إضافة الأسرار
```bash
# JWT Key
dotnet user-secrets set "Jwt:Key" "your-super-secure-jwt-key-here-minimum-256-bits"

# API Key
dotnet user-secrets set "ApiSettings:ApiKey" "your-secure-api-key-here"

# Database Password
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost; Database=social; Uid=root; Pwd=your-secure-password;"

# Email Password
dotnet user-secrets set "EmailSettings:Password" "your-email-password"
```

## 🌍 إعداد متغيرات البيئة للإنتاج

### لنظام Linux/Docker
```bash
export JWT__KEY="your-super-secure-jwt-key"
export APISETTINGS__APIKEY="your-secure-api-key"
export CONNECTIONSTRINGS__DEFAULTCONNECTION="your-production-connection-string"
export EMAILSETTINGS__PASSWORD="your-email-password"
```

### لنظام Windows
```cmd
set JWT__KEY=your-super-secure-jwt-key
set APISETTINGS__APIKEY=your-secure-api-key
set CONNECTIONSTRINGS__DEFAULTCONNECTION=your-production-connection-string
set EMAILSETTINGS__PASSWORD=your-email-password
```

## 🔐 إرشادات الأمان

### متطلبات كلمة مرور JWT Key:
- الحد الأدنى 256 بت (32 حرف)
- تحتوي على أحرف كبيرة وصغيرة وأرقام ورموز
- مثال: `Kj8%nM9$qW3&vB7#zL2@pF6!dS4^aE1*`

### متطلبات API Key:
- الحد الأدنى 64 حرف
- عشوائي تماماً
- يُفضل استخدام أدوات توليد مفاتيح آمنة

### قاعدة البيانات:
- استخدم كلمة مرور قوية
- فعّل SSL/TLS للاتصال
- قيّد الوصول من IPs محددة

## 📦 Docker Secrets (للإنتاج)

### docker-compose.yml
```yaml
version: '3.8'
services:
  social-api:
    image: social-api
    environment:
      - JWT__KEY_FILE=/run/secrets/jwt_key
      - APISETTINGS__APIKEY_FILE=/run/secrets/api_key
    secrets:
      - jwt_key
      - api_key

secrets:
  jwt_key:
    external: true
  api_key:
    external: true
```

## ☁️ Azure Key Vault (للإنتاج المتقدم)

### appsettings.Production.json
```json
{
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "TenantId": "your-tenant-id"
  }
}
```

## ⚠️ ملاحظات مهمة

1. **لا تضع أبداً** الأسرار في ملفات appsettings.json
2. **استخدم User Secrets** للتطوير المحلي فقط
3. **استخدم متغيرات البيئة** أو Azure Key Vault للإنتاج
4. **احرص على تشفير** الاتصالات بقاعدة البيانات
5. **راجع الأسرار بانتظام** وغيّرها إذا لزم الأمر