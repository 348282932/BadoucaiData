using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Badoucai.WebApi.App_Start
{
    /// <summary>
    /// 支付宝配置
    /// </summary>
    public class AlipayConfig
    {
        // 应用ID,您的APPID

        public static string app_id = "2017100909206964";

        // 商户编号（PID）

        public static string seller_id = "2088621431922923";

        // 支付宝网关

        public static string gatewayUrl = "https://openapi.alipay.com/gateway.do";

        // 商户私钥，您的原始格式RSA私钥

        public static string private_key = "MIIEpAIBAAKCAQEAw5nymM04J4niftRYeE0lGiNDRgqbdU+hzdgmYgGbRPXICzkhGGgxAjoIYMbnjD1dsBw3yUlmGFl+m/ukPOlXYGx5EgXOaYyEqwZozlDSZHjDbxKaSbjJhZJurhJiE6ZWrvSKgj6qEf5SEaNzuB71fOSq8kNR6Cpp9fElEZ1NcLjhzgLDXD1AAEVmKrYEOeU6x9MlWCcSr2RSHp14e3+vmCqpL1uD+SqMHv4LjvdCWQQFzePbYi5BHeJePF43It2Jz2lXCxPPZ9y6f2k66YsHsm/ZxdHDYP/N4A98tRm6YXntStRwtwC/ccrB/wxGXsAoAeZoIR2SOmtcEur+MzxoIwIDAQABAoIBAQC9SHDXJWC+AlTIXzztzdmlnZIwaXte3py50f3ywZM7/IyFL9ezAsDKYtZQsKrJr2jGT8g8ZWcDETfQQogA2d3QVagjpLLGuVB5IE6zuMqgp6yYA+yCguug8r9gfDGkykcAL20J9RInL3DD2OTWvD7biX1Ty4mrnb/EXIN+tDaIaO5RsYDhA/q5h4Tkxg4/Sc9dXkLD6S1Pcook7N188Vz3DK5lD0YOj7CbdP9wS7c4p2GgIlmCAP8WukNqr5UgVpzeQ8uje1w6ESePvSbTzlCGp0NRXuzCumADIgggXWFM6Y1PNs9lXOgBZBGlOGvACp8e1hORc831CkBfoUP1SVXhAoGBAPU5Uv51BpfU62PYRnAy49AKpqWy+sRFC3UjxORAf/s1CF3MXabzd1hl+RiYq8M3SdMOPieSlFoqOhzP9tO0UCL8o9R9lPOtwBPKViexfnw1Hpjn0CsTA5j1NXKVvguXOGRxzcUr5qKnirKMlKtC6e+29h33bCWMck83tBMRZW09AoGBAMwyY3OD+YcpAFa3udrz53mpVu49IFpkupMIJ5LA/Fan+aocMw2M+6dqI1dGJFPbrI/A3973+RizIY3jfK+oskJDtl+IxMoKf7fujXQwnfk3TsqLXDtX83HAKzGnuVMiXwI/t1lLFKIiHkUcwRFYrsceA8TDiay2ZoAeCZ0SD0DfAoGBAOgTS0dD62xv9iG1AdyXXOB75AD3JLnw+gnvQxwdKsHMC9HxYkRpL+55+0da3VtQDM7wMMR0xW+bfQndiyQKBrlFzaqA+yGusxRHrke8hA76mq1s3aaWRRZSIjYCHyyK/bSZu3q5tHkFBI872ktGdW0HA1+S1Eeo5lmwRvywvwOlAoGAfVcgvLNx44lDb/du2fuFRqSve6WBynqyG7aRs/9J5VdOZSDSJas4fdckwlmHywG4trTJtm+4M3UhT5sn2htO8GXn+FRXlz1CkICZy4xcK7HLZ4CLqNGf2V8AJIazt1gNwa+it+jiTXNr6ThxOliZUBgYcBsm0yFTYakOdZ+0RTkCgYBgDtlnD6NooRYn/+DKVlRAM3TvA0lKWpcHmvTApqneaJaGnp2aZt8g5+MOeM9iYf5/clgVbqzOf6TBDGbEn8apGooKtNG1C3tY2A7uHRofFX7FgFUHAgigEiO2npPWhw4Diu2tmKoB4De79Kq1FmoaCOOm/qQZGLrWPjuzaJL3Gw==";

        // 支付宝公钥,查看地址：https://openhome.alipay.com/platform/keyManage.htm 对应APPID下的支付宝公钥。

        public static string alipay_public_key = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoZUQ+zBzX/9zND2286EY0jbPIySrGdQ5SIm9Wa501n736UueFdLKbLC3ufIlGFbcbDXIFEoq/FcxmOI4G5DEJk1n5w21LScKdZV2Jg5roS0FRD4L/RBvI9MqWoHGpyZmynGmbe9lNEdIPeeRTNbVHepCwgb1cYl+l1UQTypU9qpObV4GMKNYZo/5Q9RqhE+FAWjyHq9PC0cXawJtRPL3OZDB8oNyEsXUpmEtU/DhUP6r7t68OZss0QY1zVb7ZtQbSaBm4dd1p6Jf6t/FqOb7pHGpqraKV5POzrdYHA1PQZkgzc2As415wbM8CCBq5HftO3/V+291T6vuPS68dib6wwIDAQAB";

        // 签名方式

        public static string sign_type = "RSA2";

        // 编码格式

        public static string charset = "UTF-8";
    }
}