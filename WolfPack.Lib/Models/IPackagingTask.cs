namespace WolfPack.Lib.Services
{
    public interface IPackagingTask
    {
        public PackagingTaskSettings Settings { get; set; }
        void Run();
    }
}
