﻿using NetCoreServer;
using Newtonsoft.Json;
using PayCheckServerLib.Jsons;

namespace PayCheckServerLib.Responses
{
    public class Entitlements
    {
        [HTTP("GET", "/platform/public/namespaces/pd3/users/{userId}/entitlements?limit={limit}")]
        public static bool GetUserEntitlements(HttpRequest _, PC3Server.PC3Session session)
        {
            try
            {
                var responsecreator = new ResponseCreator();
                var entitlements = JsonConvert.DeserializeObject<DataPaging<EntitlementsData>>(File.ReadAllText("./Files/Entitlements.json")) ?? throw new Exception("Entitlements is null!");
                var newentitlements = new List<EntitlementsData>();
                foreach (var entitlement in entitlements.Data)
                {
                    entitlement.UserId = session.HttpParam["userId"];
                    newentitlements.Add(entitlement);
                }
                DataPaging<EntitlementsData> payload = new()
                {
                    Data = newentitlements,
                    Paging = new()
                    {
                        First = "",
                        Last = "",
                        Next = "",
                        Previous = "",
                    }
                };
                responsecreator.SetBody(JsonConvert.SerializeObject(payload));
                session.SendResponse(responsecreator.GetResponse());
                return true;
            }
            catch (Exception ex)
            {
                Debugger.PrintError(ex.ToString());
            }
            return false;
        }
    }
}
