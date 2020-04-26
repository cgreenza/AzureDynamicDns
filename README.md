# AzureDynamicDns
Function app to remotely update DNS A and AAAA records hosted in Azure DNS. Useful if you need to remotely access your home network, but don't have a static public IP.

Usage instructions:

1. Create new function app using Azure portal/cli, use consumption plan for minimal costs
2. Enable **system assigned identity** for function app
3. Assign **DNS Zone Contributor** role access to function app's managed identity at DNS zone or record level
4. Publish function app
5. Configure function app environment variables that identify the target DNS zone resource:
   * `DNS_SUBSCRIPTION_ID`
   * `DNS_RESOURCE_GROUP`
   * `DNS_ZONE`
 6. Update DNS `A` (IPv4) or `AAAA` (IPv6) record by HTTP `POST` or `GET` on following URL
    `https://`*your_app*`.azurewebsites.net/api/UpdateDns?code=`*your_function_key*`&host=`*example*`&ip=`*203.0.113.1*
    whenever your IP address changes. Configure your router or other device on your network (e.g. Raspberry Pi) to do this automatically when your router's public IP address changes
 7. DNS TTL defaults to 900 seconds (15 minutes) but can be customised by appending `&ttl=` to the URL
