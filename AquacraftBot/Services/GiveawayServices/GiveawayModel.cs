using Google.Cloud.Firestore;

namespace AquacraftBot.Services.GiveawayServices
{
    [FirestoreData]
    public partial class GiveawayModel
    {
        [FirestoreProperty]
        public string gID { get; set; }
        [FirestoreProperty]
        public string PrizeTitle { get; set; }
        [FirestoreProperty]
        public int Winners { get; set; }
        [FirestoreProperty]
        public Timestamp EndAt { get; set; }
        [FirestoreProperty]
        public ulong HosterID { get; set; }
        [FirestoreProperty]
        public ulong messageID { get; set; }
        [FirestoreProperty]
        public ulong channelID { get; set; }
        [FirestoreProperty]
        public bool Ended { get; set; } = true;
    }
}
