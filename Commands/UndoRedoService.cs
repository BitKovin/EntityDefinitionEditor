using System;
using System.Collections.Generic;

namespace EntityEditor.Commands;

public interface IUndoable
{
    string Description { get; }
    void Execute();
    void Undo();
}

public class UndoRedoService
{
    private readonly Stack<IUndoable> _undoStack = new();
    private readonly Stack<IUndoable> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string UndoDescription => CanUndo ? _undoStack.Peek().Description : "";
    public string RedoDescription => CanRedo ? _redoStack.Peek().Description : "";

    public event EventHandler? HistoryChanged;

    public void Record(IUndoable command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        RelayCommand.Refresh();
    }

    // Record without executing (for inline edits already applied)
    public void Track(IUndoable command)
    {
        _undoStack.Push(command);
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        RelayCommand.Refresh();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        RelayCommand.Refresh();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.Push(cmd);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        RelayCommand.Refresh();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        RelayCommand.Refresh();
    }
}

// Generic property change command
public class PropertyChangeCommand<T> : IUndoable
{
    private readonly string _description;
    private readonly Action<T> _setter;
    private readonly T _oldValue;
    private readonly T _newValue;

    public PropertyChangeCommand(string description, Action<T> setter, T oldValue, T newValue)
    {
        _description = description;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public string Description => _description;
    public void Execute() => _setter(_newValue);
    public void Undo() => _setter(_oldValue);
}

// Composite command
public class CompositeCommand : IUndoable
{
    private readonly List<IUndoable> _commands;

    public CompositeCommand(string description, List<IUndoable> commands)
    {
        Description = description;
        _commands = commands;
    }

    public string Description { get; }
    public void Execute() { foreach (var c in _commands) c.Execute(); }
    public void Undo() { for (int i = _commands.Count - 1; i >= 0; i--) _commands[i].Undo(); }
}

// Add/Remove from collection commands
public class AddItemCommand<T> : IUndoable
{
    private readonly IList<T> _collection;
    private readonly T _item;
    private readonly int _index;

    public AddItemCommand(string description, IList<T> collection, T item, int index = -1)
    {
        Description = description;
        _collection = collection;
        _item = item;
        _index = index < 0 ? collection.Count : index;
    }

    public string Description { get; }
    public void Execute() => _collection.Insert(_index, _item);
    public void Undo() => _collection.Remove(_item);
}

public class RemoveItemCommand<T> : IUndoable
{
    private readonly IList<T> _collection;
    private readonly T _item;
    private int _index;

    public RemoveItemCommand(string description, IList<T> collection, T item)
    {
        Description = description;
        _collection = collection;
        _item = item;
        _index = collection.IndexOf(item);
    }

    public string Description { get; }
    public void Execute() { _index = _collection.IndexOf(_item); _collection.Remove(_item); }
    public void Undo() => _collection.Insert(_index, _item);
}
