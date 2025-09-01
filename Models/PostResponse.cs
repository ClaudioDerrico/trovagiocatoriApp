using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trovagiocatoriApp.Models
{
    public class PostResponse
    {
        public int id { get; set; }
        public string titolo { get; set; }
        public string provincia { get; set; }
        public string citta { get; set; }
        public string sport { get; set; }
        public string data_partita { get; set; }
        public string ora_partita { get; set; }
        public string commento { get; set; }
        public string autore_email { get; set; }
        public int? campo_id { get; set; }
        public FootballField campo { get; set; }
    }
}