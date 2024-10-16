using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF
{

    [FirestoreData]
    public class ProdutoData
    {
        [FirestoreProperty]
        public string Nome { get; set; }

        [FirestoreProperty]
        public string Tipo { get; set; }

        [FirestoreProperty]
        public string Marca { get; set; }
        
        [FirestoreProperty]
        public string Codigo{ get; set; }
        
        [FirestoreProperty]
        public int Quantidade { get; set; }

    }
}
