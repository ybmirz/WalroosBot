using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AquacraftBot.Services.SuggestionServices
{
    [FirestoreData]
    public partial class SuggestionModel
    {
        [FirestoreProperty]
        public string sID { get; set; }
        [FirestoreProperty]
        public ulong SubmitterID { get; set; }
        [FirestoreProperty]
        public string Content { get; set; }
        [FirestoreProperty]
        public ulong MessageID { get; set; }
    }
}

