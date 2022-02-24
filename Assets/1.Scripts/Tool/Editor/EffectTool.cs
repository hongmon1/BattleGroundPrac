using UnityEngine;
using UnityEditor;
using System.Text;
using UnityObject = UnityEngine.Object;
//Editor 폴더 안에 만들어야 함
//ToolWindow
/// <summary>
/// 이펙트 데이터 안 이펙트 클립의 속성을 설정할 수 있는 툴
/// </summary>
public class EffectTool : EditorWindow
{
    //UI 그리는데 필요한 변수들
    public int uiWidthLarge = 300; //UI 크기
    public int uiWidthMiddle = 200;
    private int selection = 0; //몇번째 선택했는지
    private Vector2 SP1 = Vector2.zero; //ScrollPosition
    private Vector2 SP2 = Vector2.zero;

    //툴용 이펙트 클립(보여주기 용)
    private GameObject effectSource = null;
    //이펙트 데이터
    private static EffectData effectData;

    //에디터 윈도우를 띄우는 초기화 함수
    [MenuItem("Tools/Effect Tool")] //경로
    static void Init()
    {
        effectData = ScriptableObject.CreateInstance<EffectData>();
        effectData.LoadData();

        EffectTool window = GetWindow<EffectTool>(false, "Effect Tool");
        window.Show();
    }

    private void OnGUI()
    {
        if(effectData == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical();
        {
            //상단. add,remove,copy
            UnityObject source = effectSource;
            EditorHelper.EditorToolTopLayer(effectData,ref selection, ref source, this.uiWidthMiddle);
            effectSource = (GameObject)source; //왜 게임 오브젝트를 유니티 오브젝트로 바꿨다가 다시 게임오브젝트로 바꾸는가
            //함수에서 source 부분에 어떤 오브젝트가 들어갈 지 모르기 때문에 언박싱 후 박싱
            //툴이니까 이렇게 때움 (비용이 싸지 않음)

            EditorGUILayout.BeginHorizontal();
            { 
                //중간. 데이터 목록
                EditorHelper.EditorToolListLayer(ref SP1, effectData, ref selection, ref source, this.uiWidthLarge);
                effectSource = (GameObject)source;

                //설정
                EditorGUILayout.BeginVertical();
                {
                    SP2 = EditorGUILayout.BeginScrollView(this.SP2);
                    {
                        if (effectData.GetDataCount() > 0)
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.Separator(); //한칸 띄우기
                                EditorGUILayout.LabelField("ID", selection.ToString(), GUILayout.Width(uiWidthLarge));
                                effectData.names[selection] = EditorGUILayout.TextField("이름.", effectData.names[selection], 
                                    GUILayout.Width(uiWidthLarge * 1.5f));
                                effectData.effectClips[selection].effectType = (EffectType)EditorGUILayout.EnumPopup("이펙트 타입.",
                                    effectData.effectClips[selection].effectType, GUILayout.Width(uiWidthLarge));
                                //type에 따라 세팅

                                EditorGUILayout.Separator();

                                if(effectSource==null && effectData.effectClips[selection].effectName != string.Empty)
                                {
                                    effectData.effectClips[selection].PreLoad();
                                    effectSource = Resources.Load(effectData.effectClips[selection].effectPath +
                                        effectData.effectClips[selection].effectName) as GameObject;
                                }
                                effectSource = (GameObject)EditorGUILayout.ObjectField("이펙트", this.effectSource,
                                    typeof(GameObject), false, GUILayout.Width(uiWidthLarge*1.5f));
                                //툴에다가 이펙트 끌어다넣음
                                //자동으로 이름과 경로 찾아주기
                                if (effectSource != null)
                                {
                                    effectData.effectClips[selection].effectPath = EditorHelper.GetPath(this.effectSource);
                                    effectData.effectClips[selection].effectName = effectSource.name;
                               
                                }
                                //리소스 잃음
                                else
                                {
                                    effectData.effectClips[selection].effectPath = string.Empty;
                                    effectData.effectClips[selection].effectName = string.Empty;
                                    effectSource = null;
                                }
                                EditorGUILayout.Separator();
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();

            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();
        //하단
        EditorGUILayout.BeginHorizontal();
        {
            if(GUILayout.Button("Reload Settings"))
            {
                effectData = CreateInstance<EffectData>();
                effectData.LoadData();
                selection = 0;
                this.effectSource = null;
            }
            if (GUILayout.Button("Save"))
            {
                EffectTool.effectData.SaveData();
                CreateEnumStructure();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }
        EditorGUILayout.EndHorizontal();
    }


    //EffectList.cs에 Enum 새로 추가
    //Tool에서 save 시 작동
    public void CreateEnumStructure()
    {
        string enumName = "EffectList";
        StringBuilder builder = new StringBuilder();
        builder.AppendLine();
        for(int i = 0; i < effectData.names.Length; i++)
        {
            if (effectData.names[i] != string.Empty)
            {
                builder.AppendLine("    "+effectData.names[i]+" = "+i+",");
            }
        }
        EditorHelper.CreateEnumStructure(enumName, builder);
    }
}
