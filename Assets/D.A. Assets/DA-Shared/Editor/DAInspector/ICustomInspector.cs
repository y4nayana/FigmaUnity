namespace DA_Assets.DAI
{
    public interface ICustomInspector
    {
        void Init();
        DAInspector Inspector { get; set; }
        InspectorData Data { get; set; }
    }
}