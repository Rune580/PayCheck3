﻿using PayCheckServerLib.Jsons;
using PayCheckServerLib.Responses;

namespace PayCheckServerLib
{
    public class TokenHelper
    {
        public struct Token
        {
            public string Name;
            public string PlatformId;
            public string UserId;
            public TokenPlatform PlatformType;
            public bool IsAccessToken;
        }

        public enum TokenPlatform
        {
            Unknow,
            Steam,
            Device,
            EOS,
            EpicGames,
            PSN,
            XBOX,
            Live
        }

        public static Token GetTokenFromPlatform(string platformId, TokenPlatform platformType)
        {
            if (!Directory.Exists("Tokens")) { Directory.CreateDirectory("Tokens"); }
            var files = Directory.GetFiles("Tokens");
            foreach (var file in files)
            {
                var token = ReadToken(File.ReadAllText(file));

                if (token.PlatformType == platformType && token.PlatformId == platformId && token.IsAccessToken)
                    return token;
            }
            var newtoken = GenerateNewToken(platformId,"DefaultUser", platformType);
            return newtoken;
        }

        public static bool IsUserIdExist(string UserId)
        {
            if (!Directory.Exists("Tokens")) { Directory.CreateDirectory("Tokens"); }
            return (File.Exists($"Tokens/{UserId}_AccessToken") || File.Exists($"Tokens/{UserId}_RefreshToken"));
        }

        public static Token GenerateNewTokenFromUser(User User, TokenPlatform platform = TokenPlatform.Steam, bool IsAccessToken = true)
        {
            return new()
            {
                Name = User.UserData.DisplayName,
                PlatformId = User.UserData.PlatformUserIds[platform.ToString().ToLower()],
                UserId = User.UserData.UserId,
                PlatformType = platform,
                IsAccessToken = IsAccessToken
            };
        }


        public static Token GenerateNewToken(string PlatformId, string Name = "DefaultUser", TokenPlatform platform = TokenPlatform.Steam, bool IsAccessToken = true)
        {
            return new()
            {
                Name = Name,
                PlatformId = PlatformId,
                UserId = UserIdHelper.CreateNewID(),
                PlatformType = platform,
                IsAccessToken = IsAccessToken
            };
        }

        public static Token GenerateFromSteamToken(string platform_token, string Name = "DefaultUser", bool IsAccessToken = true)
        {
            return new()
            {
                Name = Name,
                PlatformId = UserIdHelper.GetSteamIDFromAUTH(platform_token),
                UserId = UserIdHelper.CreateNewID(),
                PlatformType = TokenPlatform.Steam,
                IsAccessToken = IsAccessToken
            };
        }

        public static byte[] TokenToBArray(Token token)
        {
            MemoryStream ms = new();
            var bname = System.Text.Encoding.UTF8.GetBytes(token.Name);
            ms.Write(BitConverter.GetBytes(bname.Length));
            ms.Write(bname);

            var ptype = (int)token.PlatformType;
            ms.Write(BitConverter.GetBytes(ptype));

            switch (token.PlatformType)
            {
                case TokenPlatform.Unknow:
                    var bstr = System.Text.Encoding.UTF8.GetBytes(token.PlatformId);
                    ms.Write(BitConverter.GetBytes(bstr.Length));
                    ms.Write(bstr);
                    break;
                case TokenPlatform.Steam:
                    ms.Write(BitConverter.GetBytes(ulong.Parse(token.PlatformId)));
                    break;
                case TokenPlatform.Device:
                    var bplat = Convert.FromHexString(token.PlatformId);
                    ms.Write(BitConverter.GetBytes(bplat.Length));
                    ms.Write(bplat);
                    break;
                default:
                    break;
            }

            var buid = System.Text.Encoding.UTF8.GetBytes(token.UserId);
            ms.Write(BitConverter.GetBytes(buid.Length));
            ms.Write(buid);
            ms.Write(BitConverter.GetBytes(token.IsAccessToken));
            return ms.ToArray();
        }


        public static void StoreToken(Token token)
        {
            if (!Directory.Exists("Tokens")) { Directory.CreateDirectory("Tokens"); }
            string acctoken = token.IsAccessToken ? "AccessToken" : "RefreshToken";
            File.WriteAllText("Tokens/" + token.UserId + "_" + acctoken, token.ToBase64());
        }

        public static Token ReadTokenFile(string UserId, bool IsAccessToken = true)
        {
            string acctoken = IsAccessToken ? "AccessToken" : "RefreshToken";
            var text = File.ReadAllText($"Tokens/{UserId}_{acctoken}");
            return ReadToken(text);
        }

        public static (Token AccessToken, Token RefleshToken) ReadFromHeader(Dictionary<string, string> kv)
        {
            try
            {
                if (!kv.ContainsKey("cookie"))
                {
                    Debugger.PrintError("No cookie in header!");
                }
                var cookieSplit = kv["cookie"].Split("; ");
                var AccessToken = cookieSplit[0].Split("access_token=")[1];
                var RefleshToken = cookieSplit[1].Split("refresh_token=")[1];
                return (ReadToken(AccessToken), ReadToken(RefleshToken));
            }
            catch (Exception ex)
            {
                Debugger.PrintError(ex.ToString());
            }
            return (new Token(), new Token());
        }


        public static Token ReadToken(string base64)
        {
            var b64 = Convert.FromBase64String(base64);

            var bname_l = BitConverter.ToInt32(b64[0..4]);

            var name = System.Text.Encoding.UTF8.GetString(b64[4..(4 + bname_l)]);

            var platType = (TokenPlatform)BitConverter.ToInt32(b64[(4 + bname_l)..(8 + bname_l)]);


            int lastPost = (8 + bname_l);
            string PlatformId = "";
            switch (platType)
            {
                case TokenPlatform.Unknow:
                    int uleng = BitConverter.ToInt32(b64[lastPost..(lastPost + 4)]);
                    PlatformId = System.Text.Encoding.UTF8.GetString(b64[(lastPost + 4)..(4 + lastPost + uleng)]);
                    lastPost = 4 + lastPost + uleng;
                    break;
                case TokenPlatform.Steam:
                    PlatformId = BitConverter.ToUInt64(b64[lastPost..(lastPost + 8)]).ToString();
                    lastPost = 8 + lastPost;
                    break;
                case TokenPlatform.Device:
                    uleng = BitConverter.ToInt32(b64[lastPost..(lastPost + 4)]);
                    PlatformId = Convert.ToHexString(b64[(lastPost + 4)..(4 + lastPost + uleng)]);
                    lastPost = 4 + lastPost + uleng;
                    break;
                default:
                    break;
            }
            var buid_l = BitConverter.ToInt32(b64[lastPost..(4 + lastPost)]);
            var buid = System.Text.Encoding.UTF8.GetString(b64[(4 + lastPost)..(4 + lastPost + buid_l)]);
            var iAcc = BitConverter.ToBoolean(b64[(4 + lastPost + buid_l)..]);

            return new()
            {
                Name = name,
                PlatformId = PlatformId,
                UserId = buid,
                PlatformType = platType,
                IsAccessToken = iAcc
            };
        }



    }

    public static class TokenExt
    {
        public static string ToPrint(this TokenHelper.Token token)
        {
            return $"Name: {token.Name}, PlatformId: {token.PlatformId}, PlatformType: {token.PlatformType}, UserId: {token.UserId}, IsAccessToken: {token.IsAccessToken}";
        }

        public static byte[] ToBytes(this TokenHelper.Token token)
        {
            return TokenHelper.TokenToBArray(token);
        }

        public static string ToBase64(this TokenHelper.Token token)
        {
            return Convert.ToBase64String(TokenHelper.TokenToBArray(token));
        }

    }
}
