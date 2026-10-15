namespace SnowballGame.Presentation
{
    /// <summary>Something the demo-tape recorder can play frame by frame.</summary>
    public interface IDemoTape
    {
        bool Finished { get; }
        float Seconds { get; }
        void Advance(float dt);
        void PrepareRender();
        void Dispose();
    }
}
