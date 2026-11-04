using System;
using System.Collections.Generic;
using System.Linq;

namespace AntColony.Core
{
    [Serializable] public sealed class WorkContribution
    {
        [Serializable] public sealed class Entry { public string id; public float work; }
        public float total;
        public List<Entry> qualified = new List<Entry>();
        public void Add(string id, float work, bool diamond)
        {
            if (!(work > 0) || float.IsInfinity(work)) return;
            total += work;
            if (!diamond || string.IsNullOrEmpty(id)) return;
            var entry = qualified.Find(e => e.id == id);
            if (entry == null) qualified.Add(new Entry { id = id, work = work }); else entry.work += work;
        }
        public string Majority(float required) => required > 0 ? qualified.FirstOrDefault(e => e.work + .0001f >= required * .75f)?.id : null;
        public bool Valid => !float.IsNaN(total) && !float.IsInfinity(total) && total >= 0 && qualified != null && qualified.Count <= 1000
            && qualified.All(e => e != null && !string.IsNullOrEmpty(e.id) && e.id.Length <= 1000 && e.work > 0 && !float.IsInfinity(e.work))
            && qualified.Select(e => e.id).Distinct().Count() == qualified.Count && qualified.Sum(e => (double)e.work) <= total + .01;
    }

    [Serializable] public sealed class SoleWorker
    {
        public string id;
        public bool mixed, diamond;
        public void Note(string worker, bool eligible)
        {
            if (string.IsNullOrEmpty(worker)) { mixed = true; return; }
            if (string.IsNullOrEmpty(id)) { id = worker; diamond = eligible; }
            else { mixed |= id != worker; diamond &= eligible; }
        }
        public bool Empowered => !mixed && diamond && !string.IsNullOrEmpty(id);
        public bool Valid => id == null || id.Length <= 1000;
    }
}
