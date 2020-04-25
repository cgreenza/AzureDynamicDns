using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Fluent;
using System.Net;
using System.Net.Sockets;

namespace AzureDynamicDns
{
    public static class UpdateDns
    {
        [FunctionName("UpdateDns")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            AzureCredentialsFactory factory = new AzureCredentialsFactory();
            MSILoginInformation msi = new MSILoginInformation(MSIResourceType.AppService);
            AzureCredentials credentials = factory.FromMSI(msi, AzureEnvironment.AzureGlobalCloud);

            var authenticated = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Authenticate(credentials);

            var dnszone = await authenticated
                .WithSubscription(GetSetting("DNS_SUBSCRIPTION_ID"))
                .DnsZones.GetByResourceGroupAsync(GetSetting("DNS_RESOURCE_GROUP"), GetSetting("DNS_ZONE"));

            if (!IPAddress.TryParse(req.Query["ip"], out IPAddress ipAddress))
                return new BadRequestObjectResult("Invalid IP Address");

            var host = req.Query["host"];
            if (string.IsNullOrEmpty(host))
                return new BadRequestObjectResult("Invalid host");

            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    await dnszone.Update().DefineARecordSet(host).WithIPv4Address(ipAddress.ToString()).Attach().ApplyAsync();
                    break;
                case AddressFamily.InterNetworkV6:
                    await dnszone.Update().DefineAaaaRecordSet(host).WithIPv6Address(ipAddress.ToString()).Attach().ApplyAsync();
                    break;
                default:
                    return new BadRequestObjectResult($"Unknown AddressFamily {ipAddress.AddressFamily}");
            }

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
