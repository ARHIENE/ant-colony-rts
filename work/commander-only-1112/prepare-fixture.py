from pathlib import Path
p=Path('AgentScripts/KiteUser1112.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('  var local=SceneObjects.Any<LocalIncursions>();');b=s.index('  me.PersonalState.equipment.RemoveAll',a)
s=s[:a]+'''  var local=SceneObjects.Any<LocalIncursions>();var center=home+new Vector3(12,0,-13);
  var spawn=typeof(LocalIncursions).GetMethod("RestoreSpawn",BindingFlags.Instance|BindingFlags.NonPublic);
  enemy=(WildMonster)spawn.Invoke(local,new object[]{false,center+Vector3.right*5});
  otherEnemy=(WildMonster)spawn.Invoke(local,new object[]{false,center+new Vector3(4,0,18)});
  foreach(var e in new[]{enemy,otherEnemy}){e.MakeRaider();e.gameObject.SetActive(true);}
  Warp(me,center+Vector3.left*5);Warp(enemy,center+Vector3.right*5);
  Warp(other,center+new Vector3(0,0,18));Warp(otherEnemy,other.Position+Vector3.right*4);Warp(team[2],home+new Vector3(-25,0,0));
'''+s[b:];p.write_text(s,encoding='utf-8')
