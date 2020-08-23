using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace UploadImageHistoria
{
    public static class UploadImageHistoria
    {
        [FunctionName("UploadImageHistoria")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var archivo = req.Form.Files["archivo"];
                var extension = Path.GetExtension(archivo.FileName);
                var contentType = archivo.ContentType;
                var azurecon = Environment.GetEnvironmentVariable("AzureStorage");

                using (var memorystream = new MemoryStream())
                {
                    var cuenta = CloudStorageAccount.Parse(azurecon);
                    var cliente = cuenta.CreateCloudBlobClient();
                    var contenedorRef = cliente.GetContainerReference("historias");

                    await archivo.CopyToAsync(memorystream);
                    var contenido = memorystream.ToArray();

                    await contenedorRef.CreateIfNotExistsAsync();
                    await contenedorRef.SetPermissionsAsync(new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });

                    var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                    var blob = contenedorRef.GetBlockBlobReference(nombreArchivo);
                    await blob.UploadFromByteArrayAsync(contenido, 0, contenido.Length);
                    blob.Properties.ContentType = contentType;
                    await blob.SetPropertiesAsync();
                    return blob.Uri.ToString();
                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                log.LogError(ex.Message);
                return ex.Message;
            }
        }
    }
}
