using Google.Cloud.Firestore;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF.src.Services
{

    internal static class DatabaseConnect
    {
        static string fireconfig = @"{
        ""type"": ""service_account"",
        ""project_id"": ""radiadoreslemos-ea8c6"",
        ""private_key_id"": ""9489539c8ccc5d7dc175b80bc42d69d5023e268c"",
        ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCi6sIjvaKqiuLq\n4K0rzQ4IXshVBJBiBH2Ux95XTIgFpJqyx1lxN0dMyehAG2ccbE5Dffj85xhV5TT8\nVy/gpKrPBqEtirRYYRqy392dUkYonimR2BER/q+JExzJ/LaQsvKzlfhn6jnSqTVv\npaYwXO+3URcxEpvZWoWRYyH9qDKS0SZ9XFPbtUE+GApPt3f69hxdqs2avJ9hK4OD\n22vI+iSf/ne0JdW+Zn+jVsLCpEly0+94RklyfCPob/b4vnMNd/T7ohiNriuH7Xir\nLe70zlEL1sgAFElq48gWDMdBG+fHG6mXlmUaPgCzS8skxw17yOocitrR4JV/e2lD\nnXMmsFfnAgMBAAECggEAT7+L3fP3mvTWhDQMAMtlCZrgBKHxzVE2aex2fZRUZzK+\nmTH1KfLtv3x8aFkhnau0mdwh1CaJZo6G49kH8jaY+DNeFY12n2aVK6di854xArP9\nVEuIe58IrRhCeOtwMJ+wJ1GLoc5plKHIqwjSs4ziuQEEUbyytnBVvqfgnSrG6s+W\n3U43vsUK4isoVFUAbeMINAvERlRN5ozDnxIf1xghcx8+cwAzKWYgVDBDkH714zYe\n2e3aBJJhpmvzRToxuC2f3cJD3NaYsRMkE4V55TqLKm17A5wMsBJXrrATD+fJFtTo\nMwro3RB1tlxUqGOo8Le9eNhb3D0YzoCTPzLs5+wgXQKBgQDhHHD77yC+nj//QiHb\nuIcyOdEJsyVQBiVC9+vcByv3dkM+Nosrv22YbeglA+ikXAdhyRQFilQ5KTztR/aA\n4qoMrwbyNv2OIJaNa3snhJ6MXGZ87lbgAFKWOB1wY1voaHlN2PnP+YijzCgzWbXN\nfYhvQEB033WxPLfbGxddRQClvQKBgQC5RZp2Z1AlHzYQC2pMGRn+2e/Two9AniiB\nLWdUN5LHCbPOgX07l8offhvIVPNt8kdiSwnnOYEMB1phn7lnKXLwnYFn0jIOlu3c\nrRpLmaF9tkrP3tjvqFj2VBZvWk9TNLEWzWrjZy9ABFllvtDPN8UNivbtM7CPXZXa\nL1JAQ6G0cwKBgCQOSyZ/Ia6GaFe5PvUTdEweKJY2JHbR1SwJy7RdTbSAM7sGP3pN\nWf99Mx6ipqOUvfXyoAtXIbBaI5EZ4qi4JWaMrj8jga8/Fv4lxf8JZd+zeRLvleih\nBJlc+ZIjx/fMrAlFBJZEMJeTvqii6NS2E6FGGEzf8djmkcg9aZudzsG9AoGATGyz\nzMfNPaLkUDYFQSLRoFkSHw4QvZ0AJFkwWIMcHtKXw0WS/TQeAmOo3jh8ugvI+njt\nut3zp5yY4dBbUHy+lxbBvKvuTipgMDmPsUPMY+kAb0MDxchx+hqxrnlYY4BG1Jsj\nzm5QBV5F6jyOMgxVUsLSHQLHgDwghoIisbO0TpUCgYAfh0AtMsE2SxrWPxiK8r7p\n6438eEMzBa6cUp18P4GDnEp+6cJdg8AcOvU3hkjUn+GNwX02hvedcLAAfDwjJmvg\n0Hs+AVFfhicf2aHm7ve7pDnsJEGfiledhpy3TGvaLKU5nuGTdKmNCRwRvGKxuy6i\nqToZQGP/HVmusUFowaAtXA==\n-----END PRIVATE KEY-----\n"",
        ""client_email"": ""firebase-adminsdk-s0t0o@radiadoreslemos-ea8c6.iam.gserviceaccount.com"",
        ""client_id"": ""103998958035862990612"",
        ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
        ""token_uri"": ""https://oauth2.googleapis.com/token"",
        ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
        ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-s0t0o%40radiadoreslemos-ea8c6.iam.gserviceaccount.com"",
        ""universe_domain"": ""googleapis.com""
        }";

        static string filepath = "";
        public static bool IsConnected { get; set; } = false;

        public static FirestoreDb? Database { get; private set; }

        public static void SetEnvironmentVarible()
        {
            filepath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + ".json";
            File.WriteAllText(filepath, fireconfig);
            File.SetAttributes(filepath, FileAttributes.Hidden);
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);
            Database = FirestoreDb.Create("radiadoreslemos-ea8c6");
            File.Delete(filepath);
        }

        public static void TestConnection()
        {
            var collection = Database?.Collection("Test");

            try
            {
                // Adiciona um documento de teste
                var docRef = collection?.AddAsync(new { Test = "Test" }).Result;
                IsConnected = true;

                // Remove o documento de teste
                docRef?.DeleteAsync().Wait();
            }
            catch (Exception)
            {
                IsConnected = false;
            }
        }

        public static void Disconnect()
        {
            Database = null;
            IsConnected = false;
        }
    }
}
