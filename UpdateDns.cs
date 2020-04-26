using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using System.Collections.Generic;

namespace AzureDynamicDns
{
    public static class UpdateDns
    {
        [FunctionName("UpdateDns")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (!IPAddress.TryParse(req.Query["ip"], out IPAddress ipAddress))
                return new BadRequestObjectResult("Invalid IP Address");

            var host = req.Query["host"];
            if (string.IsNullOrEmpty(host))
                return new BadRequestObjectResult("Invalid host");

            var ttl = req.Query["ttl"];
            long ttlSeconds;
            if (string.IsNullOrEmpty(ttl))
                ttlSeconds = 15 * 60; // default to 15 minutes
            else if (!long.TryParse(ttl, out ttlSeconds))
                return new BadRequestObjectResult($"Invalid ttl {ttl}");

            var tokenprovider = new AzureServiceTokenProvider();
            var token = await tokenprovider.GetAccessTokenAsync("https://management.azure.com/");
            var dnsClient = new DnsManagementClient(new TokenCredentials(token)) {
                SubscriptionId = GetSetting("DNS_SUBSCRIPTION_ID")
            };

            var recordSet = new RecordSet();
            recordSet.TTL = ttlSeconds;

            RecordType recordType;
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    recordType = RecordType.A;
                    recordSet.ARecords = new List<ARecord>();
                    recordSet.ARecords.Add(new ARecord(ipAddress.ToString()));
                    break;

                case AddressFamily.InterNetworkV6:
                    recordType = RecordType.AAAA;
                    recordSet.AaaaRecords = new List<AaaaRecord>();
                    recordSet.AaaaRecords.Add(new AaaaRecord(ipAddress.ToString()));
                    break;

                default:
                    return new BadRequestObjectResult($"Unknown AddressFamily {ipAddress.AddressFamily}");
            }

            await dnsClient.RecordSets.CreateOrUpdateAsync(
                GetSetting("DNS_RESOURCE_GROUP"),
                GetSetting("DNS_ZONE"),
                host, recordType, recordSet);

            var message = $"Successfully updated {host} to {ipAddress}";
            log.LogInformation(message);

            return new OkObjectResult(message);
        }

        private static string GetSetting(string environmentVariable)
        {
            return System.Environment.GetEnvironmentVariable(environmentVariable, EnvironmentVariableTarget.Process);
        }
    }
}
