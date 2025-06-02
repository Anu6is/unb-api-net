namespace UnbelievaBoat.Net.Common
{
    public sealed record OkStatus
    {
        public static readonly OkStatus Instance = new OkStatus();
        private OkStatus() {}
    }
}
