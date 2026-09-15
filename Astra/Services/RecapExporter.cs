using System.IO;
using Astra.Models;
using Newtonsoft.Json;

namespace Astra.Services
{
    public class RecapExporter
    {
        public string ToJson(RecapData recap)
        {
            return JsonConvert.SerializeObject(recap, Formatting.Indented);
        }

        public void ExportToFile(RecapData recap, string filePath)
        {
            File.WriteAllText(filePath, ToJson(recap));
        }
    }
}
