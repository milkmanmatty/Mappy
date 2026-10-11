namespace Mappy.Operations
{
    using System;
    using System.Collections.Generic;

    public class OperationManager
    {
        private readonly Stack<IReplayableOperation> undoStack = new Stack<IReplayableOperation>();
        private readonly Stack<IReplayableOperation> redoStack = new Stack<IReplayableOperation>();
        private readonly Stack<bool> undoAffectsMark = new Stack<bool>();
        private readonly Stack<bool> redoAffectsMark = new Stack<bool>();

        private bool canUndo;
        private bool canRedo;

        private int markCounter;
        private bool isMarked = true;

        public event EventHandler CanUndoChanged;

        public event EventHandler CanRedoChanged;

        public event EventHandler IsMarkedChanged;

        public bool IsMarked
        {
            get => this.isMarked;

            private set
            {
                if (value != this.isMarked)
                {
                    this.isMarked = value;
                    this.OnIsMarkedChanged();
                }
            }
        }

        public bool CanUndo
        {
            get => this.canUndo;

            private set
            {
                var old = this.CanUndo;
                this.canUndo = value;
                if (value != old)
                {
                    this.OnCanUndoChanged();
                }
            }
        }

        public bool CanRedo
        {
            get => this.canRedo;

            private set
            {
                var old = this.CanRedo;
                this.canRedo = value;
                if (value != old)
                {
                    this.OnCanRedoChanged();
                }
            }
        }

        private int MarkCounter
        {
            get => this.markCounter;

            set
            {
                this.markCounter = value;
                this.IsMarked = value == 0;
            }
        }

        public void SetNowAsMark()
        {
            this.MarkCounter = 0;
        }

        public void Execute(IReplayableOperation op)
        {
            this.Execute(op, true);
        }

        public void Execute(IReplayableOperation op, bool affectsMark)
        {
            op.Execute();
            this.Push(op, affectsMark);
        }

        public void Push(IReplayableOperation op)
        {
            this.Push(op, true);
        }

        public void Push(IReplayableOperation op, bool affectsMark)
        {
            this.redoStack.Clear();
            this.redoAffectsMark.Clear();
            this.undoStack.Push(op);
            this.undoAffectsMark.Push(affectsMark);

            if (affectsMark)
            {
                this.MarkCounter += 1;
            }

            this.RefreshProperties();
        }

        public void Undo()
        {
            var op = this.undoStack.Pop();
            var affectsMark = this.undoAffectsMark.Pop();
            op.Undo();
            this.redoStack.Push(op);
            this.redoAffectsMark.Push(affectsMark);

            if (affectsMark)
            {
                this.MarkCounter -= 1;
            }

            this.RefreshProperties();
        }

        public void Redo()
        {
            var op = this.redoStack.Pop();
            var affectsMark = this.redoAffectsMark.Pop();
            op.Execute();
            this.undoStack.Push(op);
            this.undoAffectsMark.Push(affectsMark);

            if (affectsMark)
            {
                this.MarkCounter += 1;
            }

            this.RefreshProperties();
        }

        public void Clear()
        {
            this.undoStack.Clear();
            this.redoStack.Clear();
            this.undoAffectsMark.Clear();
            this.redoAffectsMark.Clear();

            this.RefreshProperties();

            this.MarkCounter = 0;
        }

        public IReplayableOperation PeekUndo()
        {
            return this.undoStack.Peek();
        }

        public void Replace(IReplayableOperation op)
        {
            this.undoStack.Pop();
            this.undoStack.Push(op);
        }

        private void RefreshProperties()
        {
            this.CanRedo = this.redoStack.Count != 0;
            this.CanUndo = this.undoStack.Count != 0;
        }

        private void OnCanRedoChanged()
        {
            this.CanRedoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnCanUndoChanged()
        {
            this.CanUndoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnIsMarkedChanged()
        {
            this.IsMarkedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
