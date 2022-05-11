using BepInEx;
using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LordAshes
{
    public partial class MassMovePlugin : BaseUnityPlugin
    {
        public static bool alt = false;
        public static bool control = false;
        public static CreatureGuid followMode = CreatureGuid.Empty;

        public static Dictionary<CreatureBoardAsset,Vector3> selected = new Dictionary<CreatureBoardAsset,Vector3>();

        public static Dictionary<CreatureBoardAsset, Vector3> offset = new Dictionary<CreatureBoardAsset, Vector3>();

        public static Dictionary<CreatureBoardAsset, List<Vector3>> path = new Dictionary<CreatureBoardAsset, List<Vector3>>();


        [HarmonyPatch(typeof(CreatureMoveBoardTool), "PickUp")]
        public static class PatcheCreatureMoveBoardToolPickup
        {
            public static bool Prefix()
            {
                return true;
            }

            public static void Postfix(ref CreatureBoardAsset ____pickupObject)
            {
                if (!control && !alt)
                {
                    selected.Clear();
                }
                if (!selected.ContainsKey(____pickupObject))
                {
                    if (alt && selected.Count>=1)
                    {
                        Vector3 point1 = selected.ElementAt(selected.Count - 1).Value;
                        Vector3 point2 = ____pickupObject.LastDropLocation;
                        foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                        {
                            if(Math.Min(point1.x,point2.x) <= asset.LastPlacedPosition.x)
                            {
                                if (Math.Max(point1.x, point2.x) >= asset.LastPlacedPosition.x)
                                {
                                    if (Math.Min(point1.z, point2.z) <= asset.LastPlacedPosition.z)
                                    {
                                        if (Math.Max(point1.z, point2.z) >= asset.LastPlacedPosition.z)
                                        {
                                            if (!selected.ContainsKey(asset))
                                            {
                                                selected.Add(asset, new Vector3(asset.LastPlacedPosition.x, asset.LastPlacedPosition.y, asset.LastPlacedPosition.z));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        selected.Add(____pickupObject, new Vector3(____pickupObject.LastPlacedPosition.x, ____pickupObject.LastPlacedPosition.y, ____pickupObject.LastPlacedPosition.z));
                    }
                }
                foreach(CreatureBoardAsset crit in CreaturePresenter.AllCreatureAssets)
                {
                    if (selected.ContainsKey(crit)) { crit.Select(); } else { crit.Deselect(); }
                }
                Debug.Log("Mass Move Plugin: Added Asset " + ____pickupObject.Name + " (" + control + ", Selected: "+selected.Count+")");
            }
        }

        [HarmonyPatch(typeof(MovableBoardAsset), "Drop")]
        public static class PatchMovableBoardAssetDrop
        {
            static bool dragInProgress = false;

            public static bool Prefix(Vector3 dropDestination, float height)
            {
                if (followMode != CreatureGuid.Empty && followMode != LocalClient.SelectedCreatureId)
                {
                    Debug.Log("Mass Move Plugin: Switching Leader To " + LocalClient.SelectedCreatureId);
                    StartLeading();
                }
                return true;
            }

            public static void Postfix(Vector3 dropDestination, float height)
            {
                CreatureBoardAsset _pickupObject = null;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out _pickupObject);
                Debug.Log("Mass Move Plugin: Moved Asset " + _pickupObject.Name + " From " + selected[_pickupObject] + " To " + dropDestination);
                if (!dragInProgress)
                {
                    if (followMode == CreatureGuid.Empty && selected.Count>1)
                    {
                        LeaderDragsOthers(dropDestination, _pickupObject);
                    }
                    else
                    {
                        LeaderShowsPath(dropDestination, _pickupObject);
                    }
                }
            }

            public static void StartLeading()
            {
                Debug.Log("Mass Move Plugin: Leader Starts Leading");
                if (MassMovePlugin.automaticFormationSave.Value) { SaveFormation(); }
                followMode = LocalClient.SelectedCreatureId;
                path.Clear();
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(followMode, out asset);
                if (asset != null)
                {
                    foreach (CreatureBoardAsset selectedAsset in selected.Keys)
                    {
                        if (selectedAsset.CreatureId != LocalClient.SelectedCreatureId)
                        {
                            Debug.Log("Mass Move Plugin: Generating Path For " + selectedAsset.Name + " (" + selectedAsset.CreatureId + ") From " + selectedAsset.LastPlacedPosition + " To " + asset.LastPlacedPosition);
                            path.Add(selectedAsset, BuildPath(selectedAsset.LastPlacedPosition, asset.LastPlacedPosition));
                        }
                    }
                }
            }

            public static void EndLeading()
            {
                Debug.Log("Mass Move Plugin: Leader Stops Leading");
                path.Clear();
                for(int s=0; s<selected.Keys.Count; s++)
                {
                    selected[selected.Keys.ElementAt(s)] = new Vector3(selected.Keys.ElementAt(s).LastPlacedPosition.x, selected.Keys.ElementAt(s).LastPlacedPosition.y, selected.Keys.ElementAt(s).LastPlacedPosition.z);
                }
                followMode = CreatureGuid.Empty;
            }

            public static void SaveFormation()
            {
                Debug.Log("Mass Move Plugin: Saving Formation");
                offset.Clear();
                CreatureBoardAsset leader;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out leader);
                if (leader != null)
                {
                    foreach (CreatureBoardAsset selectedAsset in selected.Keys)
                    {
                        offset.Add(selectedAsset, (leader.LastPlacedPosition - selectedAsset.LastPlacedPosition));
                        Debug.Log("Mass Move Plugin: Asset "+ selectedAsset.Name+" Has An Offset Of "+ offset[selectedAsset]);
                    }
                }
            }

            public static void RestoreFormation()
            {
                Debug.Log("Mass Move Plugin: Restoring Formation");
                dragInProgress = true;
                if (followMode != CreatureGuid.Empty) { EndLeading(); }
                path.Clear();
                CreatureBoardAsset leader;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out leader);
                if (leader != null)
                {
                    foreach (CreatureBoardAsset selectedAsset in selected.Keys)
                    {
                        Debug.Log("Mass Move Plugin: Asset " + selectedAsset.Name + " Plotting Path From "+ ToV3(selectedAsset.LastPlacedPosition)+" To "+(ToV3(leader.LastPlacedPosition) + offset[selectedAsset]));
                        path.Add(selectedAsset, BuildPath(ToV3(selectedAsset.LastPlacedPosition), (ToV3(leader.LastPlacedPosition) - offset[selectedAsset])));
                    }
                }
                self.StartCoroutine("AnimatePath");
                dragInProgress = false;
            }

            public static void LeaderDragsOthers(Vector3 dropDestination, CreatureBoardAsset _pickupObject)
            {
                dragInProgress = true;
                Debug.Log("Mass Move Plugin: Leader Drags Others");
                Vector3 delta = dropDestination - selected[_pickupObject];
                for (int a = 0; a < selected.Count; a++)
                {
                    CreatureBoardAsset selectedAsset = selected.Keys.ElementAt(a);
                    if (_pickupObject.CreatureId != selectedAsset.CreatureId)
                    {
                        Vector3 dragPos = new Vector3(selectedAsset.LastPlacedPosition.x, selectedAsset.LastPlacedPosition.y, selectedAsset.LastPlacedPosition.z) + delta;
                        Debug.Log("Mass Move Plugin: Drag Asset " + selectedAsset.Name + " By " + delta + " From " + selectedAsset.LastPlacedPosition + " To " + dragPos);
                        MoveAssetTo(selectedAsset, dragPos);
                    }
                    selected[selectedAsset] = selectedAsset.LastDropLocation;
                }
                dragInProgress = false;
            }

            public static void LeaderShowsPath(Vector3 pos, CreatureBoardAsset _pickupObject)
            {
                dragInProgress = true;
                Debug.Log("Mass Move Plugin: Leader Shows Path To " + path.Keys.Count + " Followers");
                for (int i = 0; i < path.Keys.Count; i++)
                {
                    Debug.Log("Mass Move Plugin: Checking Follower "+i);
                    try
                    {
                        if (path.ElementAt(i).Key.CreatureId != _pickupObject.CreatureId)
                        {
                            Debug.Log("Mass Move Plugin: Path Update For " + path.Keys.ElementAt(i).Name+ " (" + path.Keys.ElementAt(i).CreatureId+")");
                            path.ElementAt(i).Value.Add(pos);
                            Debug.Log("Mass Move Plugin: Asset " + path.Keys.ElementAt(i).Name + " Would Like To Follow Path");
                            MoveAssetAlongPath(path.Keys.ElementAt(i), path.ElementAt(i).Value);
                        }
                    }
                    catch (Exception x)
                    {
                        Debug.Log("Mass Move Plugin: LeaderShowsPath Exception");
                        Debug.LogException(x);
                    }
                }
                dragInProgress = false;
            }

            public static bool Occupied(CreatureBoardAsset _pickupObject, Vector3 pos)
            {
                foreach(CreatureBoardAsset check in CreaturePresenter.AllCreatureAssets)
                {
                    if (check.CreatureId != _pickupObject.CreatureId)
                    {
                        float dist = Vector3.Distance(check.LastPlacedPosition, pos);
                        Debug.Log("Mass Move Plugin: Distance Between Asset " + _pickupObject.Name + " Desired Position " + pos + " And " + check.Name + " At " + check.LastPlacedPosition + " Is " + dist);
                        if (dist < 0.9f) { return true; }
                    }
                }
                return false;
            }

            public static void MoveAssetTo(CreatureBoardAsset asset, Vector3 pos, bool singleMiniMove = true)
            {
                Debug.Log("Mass Move Plugin: Asset " + asset.Name + " Moved To " + pos);
                dragInProgress = singleMiniMove;
                asset.Pickup();
                asset.MoveTo(pos);
                asset.DropAtCurrentLocation();
                dragInProgress = false;
            }

            public static void MoveAssetAlongPath(CreatureBoardAsset asset, List<Vector3> path, bool singleMiniMove = true)
            {
                dragInProgress = singleMiniMove;
                Vector3 pos = path.ElementAt(0);
                if (!Occupied(asset, pos))
                {
                    Debug.Log("Mass Move Plugin: Asset " + asset.Name + " Moved To " + pos);
                    path.RemoveAt(0);
                    asset.Pickup();
                    asset.MoveTo(pos);
                    asset.DropAtCurrentLocation();
                }
                else
                {
                    Debug.Log("Mass Move Plugin: Asset " + asset.Name + " Wanted To Move To " + pos+" But Was Blocked");
                }
                dragInProgress = false;
            }

            public static List<Vector3> BuildPath(Vector3 source, Vector3 destination)
            {
                List<Vector3> steps = new List<Vector3>();
                Vector3 delta = destination - source;
                float dist = delta.magnitude;
                delta = delta / delta.magnitude;
                for(int step=1; step<=(int)dist; step++)
                {
                    steps.Add(source + (step * delta));
                }
                if(dist>(int)dist)
                {
                    steps.Add(destination);
                }
                return steps;
            }

            public static Vector3 ToV3(Unity.Mathematics.float3 f)
            {
                return new Vector3(f.x, f.y, f.z);
            }
        }

        public IEnumerator AnimatePath()
        {
            Debug.Log("Mass Move Plugin: Animating Formation");
            yield return new WaitForSeconds(0.1f);
            Debug.Log("Mass Move Plugin: Moving Assets Count is "+path.Count);
            while (path.Count>0)
            {
                for(int p=0; p<path.Count; p++)
                {
                    Debug.Log("Mass Move Plugin: Asset "+ path.ElementAt(p).Key.Name+ " Has " + path.ElementAt(p).Value.Count+" Steps Left");
                    if (path.ElementAt(p).Value.Count > 0)
                    {
                        PatchMovableBoardAssetDrop.MoveAssetAlongPath(path.ElementAt(p).Key, path.ElementAt(p).Value);
                    }
                }
                for (int p = 0; p < path.Count; p++)
                {
                    if (path.ElementAt(p).Value.Count == 0)
                    { 
                        path.Remove(path.ElementAt(p).Key);
                        p = 0;
                        if (path.Count <= 0) { break; }
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
