using System.Collections.Generic;
using System.Linq;

namespace cfcore
{
    public class Scope<T> where T : class
    {
        public Dictionary<string, T> Names { get; set; } = new Dictionary<string, T>();
        public Scope<T> Parent { get; set; }

        public Scope(Dictionary<string, T> names = null, Scope<T> parent = null)
        {
            Parent = parent;
            if (names != null) Names = names;
        }

        public T this[string name]
        {
            get => Names.ContainsKey(name) ? Names[name] : (Parent != null ? Parent[name] : null);
            set => Names[name] = value;
        }

        public bool Has(string name) => Names.ContainsKey(name) || (Parent != null && Parent.Has(name));

        public Scope<T> Up => Parent;
        public Scope<T> Down => new Scope<T>(parent: this);
    }

    public class NameScope
    {
        public List<string> Names { get; set; } = new List<string>();
        public NameScope Parent { get; set; }

        public NameScope(List<string> names = null, NameScope parent = null)
        {
            Parent = parent;
            if (names != null) Names = names;
        }

        public bool this[string name]
        {
            get => Names.Contains(name) || (Parent != null && Parent[name]);
            set { if (!Names.Contains(name)) Names.Add(name); }
        }

        public NameScope Up => Parent;
        public NameScope Down => new NameScope(parent: this);
    }

    public class ScopedList<T>
    {
        Stack<int> _levels = new Stack<int>();
        public List<T> List { get; set; } = new List<T>();

        public ScopedList(List<T> list = null)
        {
            if (list != null) List = list;
        }

        public void GoUp() => List = List.GetRange(0, _levels.Pop());
        public void GoDown() => _levels.Push(List.Count);
    }
}