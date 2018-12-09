﻿using NLog;
using Rubberduck.AddRemoveReferences;
using Rubberduck.Navigation.CodeExplorer;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.UI.AddRemoveReferences;
using Rubberduck.UI.Command;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;

namespace Rubberduck.UI.CodeExplorer.Commands
{
    public class AddRemoveReferencesCommand : CommandBase
    {
        private readonly IVBE _vbe;
        private readonly RubberduckParserState _state;
        private readonly IAddRemoveReferencesPresenterFactory _factory;
        private readonly IReferenceReconciler _reconciler;

        public AddRemoveReferencesCommand(IVBE vbe, 
            RubberduckParserState state, 
            IAddRemoveReferencesPresenterFactory factory,
            IReferenceReconciler reconciler) 
            : base(LogManager.GetCurrentClassLogger())
        {
            _vbe = vbe;
            _state = state;
            _factory = factory;
            _reconciler = reconciler;
        }

        protected override void OnExecute(object parameter)
        {
            if (parameter is CodeExplorerItemViewModel explorerItem)
            {
                if (!(Declaration.GetProjectParent(GetDeclaration(explorerItem)) is ProjectDeclaration project))
                {
                    return; 
                }

                var dialog = _factory.Create(project);
                var model = dialog.Show();
                if (model is null)
                {
                    return;
                }

                _reconciler.ReconcileReferences(model);
                _state.OnParseRequested(this);
            }
        }

        protected override bool EvaluateCanExecute(object parameter)
        {
            return GetDeclaration(parameter as CodeExplorerItemViewModel) is ProjectDeclaration;
        }

        private Declaration GetDeclaration(CodeExplorerItemViewModel node)
        {
            while (node != null && !(node is ICodeExplorerDeclarationViewModel))
            {
                node = node.Parent;
            }

            return (node as ICodeExplorerDeclarationViewModel)?.Declaration;
        }
    }
}
