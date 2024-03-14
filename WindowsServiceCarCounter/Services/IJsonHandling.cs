using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WindowsServiceCarCounter.Data;


namespace WindowsServiceCarCounter.Services
{
    public interface IJsonHandling
    {
        string Clean(String json);
        string Serialize(XmlDocument xmlDoc);
        List<Item> DeserializeObject(string json);
        void SendToDb(IEnumerable<Item> carCounter);
        void DeleteFile(string filename);
        void ChangeDate(List<Item> carCounter);
    }
}
