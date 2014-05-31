using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public partial class SoundManagerEditor : Editor {
	#region editor variables
	private AudioClip editAddSFX;
	private string groupToAdd = "";
	#endregion
	
	private void ShowSoundFX()
	{
		ShowSoundFXGroupsTitle();
		
		ShowSoundFXGroupsList();
		
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		
		ShowSoundFXTitle();
		
		ShowSoundFXList();
		
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		ShowAddSoundFX();
	}
	
	private void ShowSoundFXTitle()
	{
		EditorGUILayout.LabelField("Stored SFX", EditorStyles.boldLabel);
	}
	
	private void ShowSoundFXGroupsTitle()
	{
		EditorGUILayout.LabelField("SFX Groups", EditorStyles.boldLabel);
	}

	private void ShowSoundFXGroupsList()
	{
		EditorGUILayout.BeginVertical();
		{
			EditorGUI.indentLevel++;
			if(script.helpOn)
				EditorGUILayout.HelpBox("SFX Groups are used to:\n1. Access random clips in a set.\n2. Apply specific cap amounts to clips when using SoundManager.PlayCappedSFX." +
					"\n\n-Setting the cap amount to -1 will make a group use the default SFX Cap Amount\n\n-Setting the cap amount to 0 will make a group not use a cap at all.",MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.LabelField("Group Name:", GUILayout.ExpandWidth(true));
				EditorGUILayout.LabelField("Cap:", GUILayout.Width(40f));
				bool helpOn = script.helpOn;
				helpOn = GUILayout.Toggle(helpOn, "?", GUI.skin.button, GUILayout.Width(35f));
				if(helpOn != script.helpOn)
				{
					SoundManagerEditorTools.RegisterObjectChange("Change SFXGroup Help", script);
					script.helpOn = helpOn;
					EditorUtility.SetDirty(script);
				}
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.LabelField("", GUILayout.Width(10f));
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
			}
			EditorGUILayout.EndHorizontal();
			
			for(int i = 0 ; i < script.sfxGroups.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				{
					SFXGroup grp = script.sfxGroups[i];
					
					if(grp != null)
					{					
						EditorGUILayout.LabelField(grp.groupName, GUILayout.ExpandWidth(true));
						int specificCapAmount = grp.specificCapAmount;
						bool isAuto = false, isNo = false;
						
						if(specificCapAmount == -1)
							isAuto = true;
						else if(specificCapAmount == 0)
							isNo = true;
						
						bool newAuto = GUILayout.Toggle(isAuto, "Auto Cap", GUI.skin.button, GUILayout.ExpandWidth(false));
						bool newNo = GUILayout.Toggle(isNo, "No Cap", GUI.skin.button, GUILayout.ExpandWidth(false));
						
						if(newAuto != isAuto && newAuto)
						{
							specificCapAmount = -1;
						}
						if(newNo != isNo && newNo)
						{
							specificCapAmount = 0;
						}
						
						specificCapAmount = EditorGUILayout.IntField(specificCapAmount, GUILayout.Width(40f));
						if(specificCapAmount < -1) specificCapAmount = -1;
						
						if(specificCapAmount != grp.specificCapAmount)
						{
							SoundManagerEditorTools.RegisterObjectChange("Change Group Cap", script);
							grp.specificCapAmount = specificCapAmount;
							EditorUtility.SetDirty(script);
						}

						EditorGUILayout.LabelField("", GUILayout.Width(10f));
						GUI.color = Color.red;
						if(GUILayout.Button("X", GUILayout.Width(20f)))
						{
							RemoveGroup(i);
							return;
						}
						GUI.color = Color.white;
					}
				}
				EditorGUILayout.EndHorizontal();				
			}
			ShowAddGroup();
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
		EditorGUILayout.EndVertical();
	}
	
	private void ShowAddGroup()
	{
		EditorGUI.indentLevel += 2;
		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.LabelField("Add a Group:");
			groupToAdd = EditorGUILayout.TextField(groupToAdd, GUILayout.ExpandWidth(true));
			GUI.color = softGreen;
			if(GUILayout.Button("add", GUILayout.Width(40f)))
			{
				AddGroup(groupToAdd, -1);
				groupToAdd = "";
				GUIUtility.keyboardControl = 0;
			}
			GUI.color = Color.white;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUI.indentLevel -= 2;
	}
	
	private void ShowSoundFXList()
	{
		var expand = '\u2261'.ToString();
		GUIContent expandContent = new GUIContent(expand, "Expand/Collapse");
		
		string[] groups = GetAvailableGroups();
		EditorGUILayout.BeginVertical();
		{
			EditorGUI.indentLevel++;
			for(int i = 0 ; i < script.storedSFXs.Count ; i++)
			{
				AudioClip obj = script.storedSFXs[i];
				if(obj != null)
				{
					EditorGUILayout.BeginHorizontal();
					{
						AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(obj, typeof(AudioClip), false);
						if(newClip != obj)
						{
							if(newClip == null)
							{
								RemoveSFX(i);
								return;
							}
							else
							{
								SoundManagerEditorTools.RegisterObjectChange("Change SFX", script);
								obj = newClip;
								EditorUtility.SetDirty(script);
							}
						}
						if((script.showSFXDetails[i] && GUILayout.Button("-", GUILayout.Width(30f))) || (!script.showSFXDetails[i] && GUILayout.Button(expandContent, GUILayout.Width(30f))))
						{
							script.showSFXDetails[i] = !script.showSFXDetails[i];
						}
						
						GUI.color = Color.red;
						if(GUILayout.Button("X", GUILayout.Width(20f)))
						{
							RemoveSFX(i);
							return;
						}
						GUI.color = Color.white;
					}
					EditorGUILayout.EndHorizontal();
					
					if(script.showSFXDetails[i])
					{
						EditorGUI.indentLevel+=4;
						string clipName = obj.name;
						int oldIndex = IndexOfKey(clipName);
						if(oldIndex >= 0) // if in a group, find index
							oldIndex = IndexOfGroup(script.clipToGroupValues[oldIndex]);
						if(oldIndex < 0) // if not in a group, set it to none
							oldIndex = 0;
						else //if in a group, add 1 to index to cover for None group type
							oldIndex++;

						int newIndex = EditorGUILayout.Popup("Group:", oldIndex, groups, EditorStyles.popup);
						if(oldIndex != newIndex)
						{						
							string groupName = groups[newIndex];
							ChangeGroup(clipName, oldIndex, newIndex, groupName);
							return;
						}

						int prepoolAmount = script.sfxPrePoolAmounts[i];
						
						prepoolAmount = EditorGUILayout.IntField("Prepool Amount:", prepoolAmount);
						if(prepoolAmount != script.sfxPrePoolAmounts[i])
						{
							SoundManagerEditorTools.RegisterObjectChange("Change Prepool Amount", script);
							script.sfxPrePoolAmounts[i] = prepoolAmount;
							EditorUtility.SetDirty(script);
						}
						EditorGUI.indentLevel-=4;
					}
				}				
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
		EditorGUILayout.EndVertical();
	}
	
	private void ShowAddSoundFX()
	{
		ShowAddSoundFXDropGUI();
		
		ShowAddSoundFXSelector();
		
		string[] groups = GetAvailableGroups();
		if(groups.Length > 1)
			script.groupAddIndex = EditorGUILayout.Popup("Auto-Add To Group:", script.groupAddIndex, groups, EditorStyles.popup, GUILayout.Width(100f), GUILayout.ExpandWidth(true));
		else 
			script.groupAddIndex = 0;
		script.autoPrepoolAmount = EditorGUILayout.IntField("Auto-Prepool Amount:", script.autoPrepoolAmount);
	}
	
	private void ShowAddSoundFXDropGUI()
	{
		GUI.color = softGreen;
		EditorGUILayout.BeginVertical();
		{
			var evt = Event.current;
			
			var dropArea = GUILayoutUtility.GetRect(0f,50f,GUILayout.ExpandWidth(true));
			GUI.Box (dropArea, "Drag SFX(s) Here");
			
			switch (evt.type)
			{
			case EventType.DragUpdated:
			case EventType.DragPerform:
				if(!dropArea.Contains (evt.mousePosition))
					break;
				
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				
				if( evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();
					
					foreach (var draggedObject in DragAndDrop.objectReferences)
					{
						var aC = draggedObject as AudioClip;
						if(!aC || aC.GetType() != typeof(AudioClip))
							continue;
						
						if(AlreadyContainsSFX(aC))
							Debug.LogError("You already have that sound effect(" +aC.name+ ") attached, or a sound effect with the same name.");
						else
							AddSFX(aC, script.groupAddIndex, script.autoPrepoolAmount);
					}
				}
				Event.current.Use();
				break;
			}
		}
		EditorGUILayout.EndVertical();
		GUI.color = Color.white;
	}
	
	private void ShowAddSoundFXSelector()
	{
		EditorGUILayout.BeginHorizontal();
		{
			editAddSFX = EditorGUILayout.ObjectField("Select A SFX:", editAddSFX, typeof(AudioClip), false) as AudioClip;
					
			GUI.color = softGreen;
			if(GUILayout.Button("add", GUILayout.Width(40f)))
			{
				if(AlreadyContainsSFX(editAddSFX))
					Debug.LogError("You already have that sound effect(" +editAddSFX.name+ ") attached, or a sound effect with the same name.");
				else
					AddSFX(editAddSFX, script.groupAddIndex, script.autoPrepoolAmount);
				editAddSFX = null;
			}
			GUI.color = Color.white;
		}
		EditorGUILayout.EndHorizontal();
	}
	
	private void AddSFX(AudioClip clip, int groupIndex, int prepoolAmount)
	{
		if(clip == null) 
			return;
		SoundManagerEditorTools.RegisterObjectChange("Add SFX", script);
		script.storedSFXs.Add(clip);
		script.sfxPrePoolAmounts.Add(prepoolAmount);
		script.showSFXDetails.Add(false);
		if(script.groupAddIndex > 0)
			AddToGroup(clip.name, GetAvailableGroups()[script.groupAddIndex]);
		else
		{
			EditorUtility.SetDirty(script);
		
			SceneView.RepaintAll();
		}
	}
	
	private void RemoveSFX(int index)
	{
		SoundManagerEditorTools.RegisterObjectChange("Remove SFX", script);
		string clipName = script.storedSFXs[index].name;
		script.storedSFXs.RemoveAt(index);
		if(IsInAGroup(clipName))
			RemoveFromGroup(clipName);
		EditorUtility.SetDirty(script);
		
		SceneView.RepaintAll();
	}
	
	private void AddGroup(string groupName, int capAmount)
	{
		if(AlreadyContainsGroup(groupName))
			return;
		
		SoundManagerEditorTools.RegisterObjectChange("Add Group", script);
		script.sfxGroups.Add(new SFXGroup(groupName, capAmount));
		EditorUtility.SetDirty(script);
		
		SceneView.RepaintAll();
	}
	
	private void RemoveGroup(int index)
	{
		SoundManagerEditorTools.RegisterObjectChange("Remove Group", script);
		string groupName = script.sfxGroups[index].groupName;
		script.sfxGroups.RemoveAt(index);
		RemoveAllInGroup(groupName);
		EditorUtility.SetDirty(script);
		
		SceneView.RepaintAll();
	}
	
	private void RemoveAllInGroup(string groupName)
	{
		for( int i = 0; i < script.clipToGroupKeys.Count; i++)
			if(script.clipToGroupValues[i] == groupName)
				RemoveFromGroup(script.clipToGroupKeys[i]);
	}
	
	private string[] GetAvailableGroups()
	{
		List<string> result = new List<string>();
		result.Add("None");
		for( int i = 0; i < script.sfxGroups.Count; i++)
			result.Add(script.sfxGroups[i].groupName);
		return result.ToArray();
	}
	
	private bool IsInAGroup(string clipname)
	{
		for( int i = 0; i < script.clipToGroupKeys.Count; i++)
			if(script.clipToGroupKeys[i] == clipname)
				return true;
		return false;
	}
	
	private int IndexOfKey(string clipname)
	{
		for( int i = 0; i < script.clipToGroupKeys.Count; i++)
			if(script.clipToGroupKeys[i] == clipname)
				return i;
		return -1;
	}
	
	private int IndexOfGroup(string groupname)
	{
		for( int i = 0; i < script.sfxGroups.Count; i++)
			if(script.sfxGroups[i].groupName == groupname)
				return i;
		return -1;
	}
	
	private void ChangeGroup(string clipName, int previousIndex, int nextIndex, string groupName)
	{
		SoundManagerEditorTools.RegisterObjectChange("Change Group", script);
		if(previousIndex == 0) //wasnt in group, so add it
		{
			AddToGroup(clipName, groupName);
		}
		else if (nextIndex == 0) //was in group but now doesn't want to be
		{
			RemoveFromGroup(clipName);
		}
		else //just changing groups
		{
			int index = IndexOfKey(clipName);
			script.clipToGroupValues[index] = groupName;
			EditorUtility.SetDirty(script);
		}
	}
	
	private void AddToGroup(string clipName, string groupName)
	{
		if(IsInAGroup(clipName) || !AlreadyContainsGroup(groupName))
			return;
		script.clipToGroupKeys.Add(clipName);
		script.clipToGroupValues.Add(groupName);
		
		EditorUtility.SetDirty(script);
		
		SceneView.RepaintAll();
	}
	
	private void RemoveFromGroup(string clipName)
	{
		if(!IsInAGroup(clipName))
			return;
		int index = IndexOfKey(clipName);
			
		script.clipToGroupKeys.RemoveAt(index);
		script.clipToGroupValues.RemoveAt(index);

		EditorUtility.SetDirty(script);
		
		SceneView.RepaintAll();
	}
	
	private bool AlreadyContainsSFX(AudioClip clip)
	{
		for(int i = 0 ; i < script.storedSFXs.Count ; i++)
		{
			AudioClip obj = script.storedSFXs[i];
		
			if(obj == null) continue;
			if(obj.name == clip.name || obj == clip)
				return true;			
		}
		return false;
	}
	
	private bool AlreadyContainsGroup(string grpName)
	{
		for(int i = 0 ; i < script.sfxGroups.Count ; i++)
		{
			SFXGroup grp = script.sfxGroups[i];
		
			if(grp == null) continue;
			if(grp.groupName == grpName)
				return true;
		}
		return false;
	}
}
