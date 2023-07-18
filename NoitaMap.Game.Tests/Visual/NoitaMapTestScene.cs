using osu.Framework.Testing;

namespace NoitaMap.Game.Tests.Visual;

public partial class NoitaMapTestScene : TestScene
{
    protected override ITestSceneTestRunner CreateRunner() => new NoitaMapTestSceneTestRunner();

    private partial class NoitaMapTestSceneTestRunner : NoitaMapGameBase, ITestSceneTestRunner
    {
        private TestSceneTestRunner.TestRunner Runner;

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();
            Add(Runner = new TestSceneTestRunner.TestRunner());
        }

        public void RunTestBlocking(TestScene test) => Runner.RunTestBlocking(test);
    }
}
