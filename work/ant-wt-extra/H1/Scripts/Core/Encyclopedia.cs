using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AntColony.Save;
using UnityEngine;

namespace AntColony.Core
{
    public static class Encyclopedia
    {
        [Serializable] private class Book { public List<DiscoveryDto> entries = new List<DiscoveryDto>(); }
        private static Book book;
        private static string Path => System.IO.Path.Combine(SaveStorage.Root, "discoveries.json");
        public static IReadOnlyList<DiscoveryDto> Entries { get { EnsureLoaded(); return book.entries; } }
        private static void EnsureLoaded()
        {
            if (book != null) return;
            try { book = File.Exists(Path) ? JsonUtility.FromJson<Book>(File.ReadAllText(Path)) : new Book(); }
            catch { book = new Book(); }
            if (book?.entries == null) book = new Book();
            book.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.key));
        }
        public static void Discover(string category, string key, string title, string body)
        {
            EnsureLoaded(); if (book.entries.Any(e => e.key == key)) return;
            book.entries.Add(new DiscoveryDto { category = category, key = key, title = title, body = body });
            Persist();
        }
        public static void Merge(IEnumerable<DiscoveryDto> entries)
        { EnsureLoaded(); foreach (var e in entries) if (!book.entries.Any(x => x.key == e.key)) book.entries.Add(e); Persist(); }
        private static void Persist()
        { try { SaveStorage.WriteAtomic(Path, JsonUtility.ToJson(book, true)); } catch (Exception e) { UI.ToastManager.Show("Discovery save failed: " + e.Message); } }
        public static void ResetCache() => book = null;
    }
}
