using System;
using System.IO;
using UnityEngine;

namespace AntColony.Save
{
    // 파일 입출력만 담당한다. 게임 상태는 전혀 모른다.
    // 규칙: 임시 파일에 먼저 쓰고, 기존 파일을 .bak으로 밀어낸 뒤 교체한다.
    // 교체 중 실패해도 마지막 정상 저장(.bak 또는 기존 파일)은 남는다.
    public static class SaveStorage
    {
        private static string rootOverride;

        // 검사에서 실제 사용자 저장 파일을 건드리지 않도록 경로를 갈아끼울 수 있게 열어 둔다.
        // null이면 Application.persistentDataPath를 쓴다.
        public static string RootOverride
        {
            get => rootOverride;
            set => rootOverride = string.IsNullOrEmpty(value) ? null : value;
        }

        public static string Root => rootOverride ?? Application.persistentDataPath;

        public static string SavesFolder => Path.Combine(Root, "saves");

        public static void EnsureFolder(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        public static void WriteAtomic(string path, string contents)
        {
            EnsureFolder(path);
            var temp = path + ".tmp";
            var backup = path + ".bak";
            File.WriteAllText(temp, contents);
            if (File.Exists(path))
            {
                // File.Replace는 대상이 있을 때만 쓸 수 있고, 실패하면 원본이 그대로 남는다.
                File.Replace(temp, path, backup, true);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        public static bool TryReadText(string path, out string contents, out string error)
        {
            contents = null;
            error = null;
            try
            {
                if (!File.Exists(path)) { error = "File not found."; return false; }
                contents = File.ReadAllText(path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static bool Delete(string path)
        {
            try
            {
                var removed = false;
                foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
                    if (File.Exists(candidate)) { File.Delete(candidate); removed = true; }
                return removed;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save delete failed: " + e.Message);
                return false;
            }
        }
    }
}
