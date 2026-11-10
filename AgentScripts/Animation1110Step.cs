using System.Threading.Tasks;using UnityEditor;using UnityEngine;
public static class Animation1110Step {public static async Task<string> Main(){EditorApplication.isPaused=false;await Task.Delay(450);EditorApplication.isPaused=true;return "Advanced live game 450ms";}}
