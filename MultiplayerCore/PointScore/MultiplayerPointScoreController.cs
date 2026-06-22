using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TootTallyAccounts;
using TootTallyCore.Utils.Assets;
using TootTallyLeaderboard;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyMultiplayer.MultiplayerCore.PointScore
{
    public class MultiplayerPointScoreController : MonoBehaviour
    {
        private static Dictionary<int, MultiplayerPointScore> _idToPointScoreDict;
        private static List<SavedPointScore> _savedPointScoreList;
        private static GameObject _gameObject;
        private static bool _isInitialized;
        private static PointSceneController _pointSceneController;
        private static ScoreGraph _scoreGraph;

        public void Awake()
        {
            _gameObject = gameObject;
            _idToPointScoreDict = new Dictionary<int, MultiplayerPointScore>();
            if (_savedPointScoreList != null)
            {
                _savedPointScoreList.ForEach(InitPointScore);
                _savedPointScoreList.Clear();
            }
            _isInitialized = true;
        }

        public void Initialize(PointSceneController __instance)
        {
            _pointSceneController = __instance;
            try
            {
                _scoreGraph = _pointSceneController.scorepopupcamera.GetComponent<ScoreGraph>();
                _pointSceneController.btn_cont_canvas.transform.parent.GetComponent<Image>().raycastTarget = false; //If this is set to true, it blocks the raycast for everything outside the button canvas lol...
            }
            catch (Exception e)
            {
                Plugin.LogError($"Couldn't find ScoreGraph Component in scorepopupcamera object: {e.Message}");
                Plugin.LogError($"Trace: {e.StackTrace}");
            }
        }

        public static void AddScoreDebug() => AddScore(MultiplayerController.GetUserFromLobby(TootTallyUser.userInfo.id).id, UnityEngine.Random.Range(1, 1000000), UnityEngine.Random.value, UnityEngine.Random.Range(0, 1000), new int[] { 0, 1, 2, 3, 4 }, "S", "EZ,HD");

        public static void AddScore(int id, int score, float percent, int maxCombo, int[] noteTally, string grade, string modifiers = null)
        {
            if (_isInitialized)
                InitPointScore(id, score, percent, maxCombo, grade, noteTally, modifiers);
            else
            {
                _savedPointScoreList ??= new List<SavedPointScore>();
                _savedPointScoreList.Add(new SavedPointScore(id, score, percent, maxCombo, grade, noteTally, modifiers));
            }
        }

        private static void InitPointScore(SavedPointScore pointScore) => InitPointScore(pointScore.id, pointScore.score, pointScore.percent, pointScore.maxCombo, pointScore.grade, pointScore.noteTally, pointScore.modifiers);

        private static void InitPointScore(int id, int score, float percent, int maxCombo, string grade, int[] noteTally, string modifiers = null)
        {
            if (!_idToPointScoreDict.ContainsKey(id))
            {
                var user = MultiplayerController.GetUserFromLobby(id);
                if (user.id == 0) return;

                var pointScore = MultiplayerGameObjectFactory.CreatePointScoreCard(_gameObject.transform, new Vector2(-250, 32 * _idToPointScoreDict.Count), $"{id}PointScore").AddComponent<MultiplayerPointScore>();
                pointScore.Initialize(id, user.username, score, percent, maxCombo, grade, noteTally, modifiers, OnPointScoreClick);
                _idToPointScoreDict.Add(id, pointScore);

                var ordered = _idToPointScoreDict.Select(x => x.Value).OrderByDescending(x => x.GetScore).ToArray();
                for (int i = 0; i < ordered.Length; i++)
                    ordered[i].SetPosition(i + 1, _idToPointScoreDict.Count);
            }
        }

        public static void OnPointScoreClick(int id, string name, int score, float percent, string grade, int[] noteTally, string modifiers = null)
        {
            if (!_isInitialized || _pointSceneController == null) return;

            UpdatePointSceneData(name, score, percent, grade, noteTally, modifiers);
            UpdatePointSceneGraph(id);
        }

        public static void UpdatePointSceneData(string name, int score, float percent, string grade, int[] noteTally, string modifiers = null)
        {
            if (_pointSceneController.track_dotimages[0] == null) InitTrackArrays();

            _pointSceneController.popScoreAnim(grade.Contains('S') ? 5 : 4);
            if (TootTallyLeaderboard.Plugin.Instance.option.ShowCoolS.Value && noteTally[0] == 0 && noteTally[1] == 0 && noteTally[2] == 0)
            {
                _pointSceneController.giantscoretext.text = _pointSceneController.giantscoretextshad.text = "You Suck";
                _pointSceneController.giantscoretext.fontSize = _pointSceneController.giantscoretextshad.fontSize = 12;
                if (noteTally[3] == 0)
                    _pointSceneController.giantscorediamond.transform.Find("cool-s").GetComponent<Image>().sprite = AssetManager.GetSprite("Cool-sss.png");
                _pointSceneController.giantscorediamond.transform.Find("cool-s").gameObject.SetActive(true);
            }
            else
            {
                _pointSceneController.giantscoretext.text = _pointSceneController.giantscoretextshad.text = grade;
                _pointSceneController.giantscoretext.fontSize = _pointSceneController.giantscoretextshad.fontSize = 110;
                _pointSceneController.giantscorediamond.transform.Find("cool-s").gameObject.SetActive(false);
            }
            _pointSceneController.txt_prevhigh.text = name;
            GameObject prevHighLabel = GameObject.Find("Canvas/FullPanel/LeftLabels/PREV. HI SCORE");
            Text[] prevHighLabelText = prevHighLabel.GetComponentsInChildren<Text>();
            foreach (Text text in prevHighLabelText)
                text.text = "Player";
            _pointSceneController.txt_score.text = $"{score:n0} {percent:0.00}%";
            _pointSceneController.txt_nasties.text = noteTally[0].ToString("n0");
            _pointSceneController.txt_mehs.text = noteTally[1].ToString("n0");
            _pointSceneController.txt_okays.text = noteTally[2].ToString("n0");
            _pointSceneController.txt_nices.text = noteTally[3].ToString("n0");
            _pointSceneController.txt_perfectos.text = noteTally[4].ToString("n0");

            if (!string.IsNullOrEmpty(modifiers) && !modifiers.ToLower().Contains("none"))
                _pointSceneController.txt_trackname.text += $" [{modifiers}]";
            //TODO: Add the bar at the top stuff... complicated without access to the basegame percent calc and stuff
        }

        private static void InitTrackArrays()
        {
            for (int m = 0; m < 4; m++)
            {
                _pointSceneController.track_arrows_rects[m] = _pointSceneController.track_arrows_objs[m].transform.GetComponent<RectTransform>();
            }
            _pointSceneController.track_barempty = _pointSceneController.trackobj.transform.GetChild(0).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_barfill = _pointSceneController.trackobj.transform.GetChild(1).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_arrow = _pointSceneController.trackobj.transform.GetChild(2).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot0 = _pointSceneController.trackobj.transform.GetChild(3).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot1 = _pointSceneController.trackobj.transform.GetChild(4).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot2 = _pointSceneController.trackobj.transform.GetChild(5).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot3 = _pointSceneController.trackobj.transform.GetChild(6).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot4 = _pointSceneController.trackobj.transform.GetChild(7).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot5 = _pointSceneController.trackobj.transform.GetChild(8).gameObject.GetComponent<RectTransform>();
            _pointSceneController.track_dot0img = _pointSceneController.trackobj.transform.GetChild(3).gameObject.GetComponent<Image>();
            _pointSceneController.track_dot1img = _pointSceneController.trackobj.transform.GetChild(4).gameObject.GetComponent<Image>();
            _pointSceneController.track_dot2img = _pointSceneController.trackobj.transform.GetChild(5).gameObject.GetComponent<Image>();
            _pointSceneController.track_dot3img = _pointSceneController.trackobj.transform.GetChild(6).gameObject.GetComponent<Image>();
            _pointSceneController.track_dot4img = _pointSceneController.trackobj.transform.GetChild(7).gameObject.GetComponent<Image>();
            _pointSceneController.track_dot5img = _pointSceneController.trackobj.transform.GetChild(8).gameObject.GetComponent<Image>();
            _pointSceneController.track_dotimages[0] = _pointSceneController.track_dot0img;
            _pointSceneController.track_dotimages[1] = _pointSceneController.track_dot1img;
            _pointSceneController.track_dotimages[2] = _pointSceneController.track_dot2img;
            _pointSceneController.track_dotimages[3] = _pointSceneController.track_dot3img;
            _pointSceneController.track_dotimages[4] = _pointSceneController.track_dot4img;
            _pointSceneController.track_dotimages[5] = _pointSceneController.track_dot5img;
            _pointSceneController.track_dot0txt = _pointSceneController.track_dot0.transform.GetChild(0).gameObject.GetComponent<Text>();
            _pointSceneController.track_dot1txt = _pointSceneController.track_dot1.transform.GetChild(0).gameObject.GetComponent<Text>();
            _pointSceneController.track_dot2txt = _pointSceneController.track_dot2.transform.GetChild(0).gameObject.GetComponent<Text>();
            _pointSceneController.track_dot3txt = _pointSceneController.track_dot3.transform.GetChild(0).gameObject.GetComponent<Text>();
            _pointSceneController.track_dot4txt = _pointSceneController.track_dot4.transform.GetChild(0).gameObject.GetComponent<Text>();
            _pointSceneController.track_dot5txt = _pointSceneController.track_dot5.transform.GetChild(0).gameObject.GetComponent<Text>();
            _pointSceneController.track_dottxts[0] = _pointSceneController.track_dot0txt;
            _pointSceneController.track_dottxts[1] = _pointSceneController.track_dot1txt;
            _pointSceneController.track_dottxts[2] = _pointSceneController.track_dot2txt;
            _pointSceneController.track_dottxts[3] = _pointSceneController.track_dot3txt;
            _pointSceneController.track_dottxts[4] = _pointSceneController.track_dot4txt;
            _pointSceneController.track_dottxts[5] = _pointSceneController.track_dot5txt;
            _pointSceneController.track_p0 = _pointSceneController.track_dot0.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            _pointSceneController.track_p1 = _pointSceneController.track_dot1.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            _pointSceneController.track_p2 = _pointSceneController.track_dot2.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            _pointSceneController.track_p3 = _pointSceneController.track_dot3.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            _pointSceneController.track_p4 = _pointSceneController.track_dot4.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            _pointSceneController.track_p5 = _pointSceneController.track_dot5.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            _pointSceneController.track_ps[0] = _pointSceneController.track_p0;
            _pointSceneController.track_ps[1] = _pointSceneController.track_p1;
            _pointSceneController.track_ps[2] = _pointSceneController.track_p2;
            _pointSceneController.track_ps[3] = _pointSceneController.track_p3;
            _pointSceneController.track_ps[4] = _pointSceneController.track_p4;
            _pointSceneController.track_ps[5] = _pointSceneController.track_p5;
        }

        public static void UpdatePointSceneGraph(int id)
        {
            if (MultiplayerLiveScoreController.idToSavedNoteDataDict == null || !MultiplayerLiveScoreController.idToSavedNoteDataDict.ContainsKey(id)) return;
            for (int i = 0; i < MultiplayerLiveScoreController.idToSavedNoteDataDict[id].Count && i < GlobalVariables.gameplay_allnotescores.Count; i++)
                UpdatePointSceneGraphNode(MultiplayerLiveScoreController.idToSavedNoteDataDict[id][i][0], MultiplayerLiveScoreController.idToSavedNoteDataDict[id][i][1], GlobalVariables.gameplay_allnotescores[i][2], i);
        }

        public static void UpdatePointSceneGraphNode(float score, float mult, float timestamp, int index)
        {
            //The + 4 is because the first 4 elements contained in the scoregraph object are the grid axes and the multline object. Everything else *should* be nodes.
            if (_scoreGraph.graphpanel.transform.childCount < index + 4) return;
            var nodeRect = _scoreGraph.graphpanel.transform.GetChild(index + 4).GetComponent<RectTransform>();

            if (!nodeRect.name.Contains("node")) return; //Just for safety measures... lol

            var xMult = timestamp / _scoreGraph.lasttimestamp;
            nodeRect.anchoredPosition3D = new Vector3(xMult * _scoreGraph.panelwidth, (Mathf.Clamp(score, 50, 100) - 50f) * (_scoreGraph.nodevspacing * 2f), 0f);
            _scoreGraph.multline.SetPosition(index, new Vector3(xMult * _scoreGraph.panelwidth, Mathf.Min(mult, 10f) / 10f * _scoreGraph.panelheight, 0f));
        }

        public static void ClearSavedScores() => _savedPointScoreList?.Clear();

        public void OnDestroy()
        {
            _idToPointScoreDict = null;
            _pointSceneController = null;
            _isInitialized = false;
        }

        public class SavedPointScore
        {
            public int id, score, maxCombo;
            public int[] noteTally;
            public float percent;
            public string grade, modifiers;

            public SavedPointScore(int id, int score, float percent, int maxCombo, string grade, int[] noteTally, string modifiers = null)
            {
                this.id = id;
                this.score = score;
                this.percent = percent;
                this.maxCombo = maxCombo;
                this.grade = grade;
                this.noteTally = noteTally;
                this.modifiers = modifiers;
            }
        }
    }
}
