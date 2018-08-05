using System;
using System.Diagnostics;
using NLog;
using Rubberduck.Parsing.VBA;
using Rubberduck.UI.UnitTesting;
using Rubberduck.UnitTesting;
using System.Linq;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA.Extensions;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;

namespace Rubberduck.UI.Command
{
    /// <summary>
    /// A command that runs all Rubberduck unit tests in the VBE.
    /// </summary>
    public class RunAllTestsCommand : CommandBase
    {
        private readonly IVBE _vbe;
        private readonly ITestEngine _engine;
        private readonly TestExplorerModel _model;
        private readonly IDockablePresenter _presenter;
        private readonly RubberduckParserState _state;

        public RunAllTestsCommand(IVBE vbe, RubberduckParserState state, ITestEngine engine, TestExplorerModel model, IDockablePresenter presenter) 
            : base(LogManager.GetCurrentClassLogger())
        {
            _vbe = vbe;
            _engine = engine;
            _model = model;
            _state = state;
            _presenter = presenter;
        }

        protected override bool EvaluateCanExecute(object parameter)
        {
            // the vbe design mode requirement could also be encapsulated into the engine
            return _vbe.IsInDesignMode && _engine.CanRun();
        }

        protected override void OnExecute(object parameter)
        {
            EnsureRubberduckIsReferencedForEarlyBoundTests();

            if (!_state.IsDirty())
            {
                RunTests();
            }
            else
            {
                _model.TestsRefreshed += TestsRefreshed;
                _model.Refresh();
            }
        }

        private void EnsureRubberduckIsReferencedForEarlyBoundTests()
        {
            var projectIdsOfMembersUsingAddInLibrary = _state.DeclarationFinder.AllUserDeclarations
                .Where(member => member.AsTypeName == "Rubberduck.PermissiveAssertClass"
                                 || member.AsTypeName == "Rubberduck.AssertClass")
                .Select(member => member.ProjectId)
                .ToHashSet();
            var projectsUsingAddInLibrary = _state.DeclarationFinder
                .UserDeclarations(DeclarationType.Project)
                .Where(declaration => projectIdsOfMembersUsingAddInLibrary.Contains(declaration.ProjectId))
                .Select(declaration => declaration.Project);

            foreach (var project in projectsUsingAddInLibrary)
            {
                project?.EnsureReferenceToAddInLibrary();
            }
        }

        private void TestsRefreshed(object sender, EventArgs e)
        {
            RunTests();
        }

        private void RunTests()
        {
            _model.TestsRefreshed -= TestsRefreshed;

            var stopwatch = new Stopwatch();

            _model.ClearLastRun();
            _model.IsBusy = true;

            _presenter?.Show();

            stopwatch.Start();
            try
            {
                _engine.Run(_model.Tests.Select(vm => vm.Method));
            }
            finally
            {
                stopwatch.Stop();
                _model.IsBusy = false;
            }

            Logger.Info($"Test run completed in {stopwatch.ElapsedMilliseconds}.");
            OnRunCompleted(new TestRunEventArgs(stopwatch.ElapsedMilliseconds));
        }

        public event EventHandler<TestRunEventArgs> RunCompleted;
        protected virtual void OnRunCompleted(TestRunEventArgs e)
        {
            var handler = RunCompleted;
            handler?.Invoke(this, e);
        }
    }
    
    public class TestRunEventArgs : EventArgs
    {
        public long Duration { get; }

        public TestRunEventArgs(long duration)
        {
            Duration = duration;
        }
    }
}
