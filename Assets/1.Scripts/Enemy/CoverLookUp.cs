using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 숨을만한 곳을 찾아주는 컴포넌트
/// 플레이어보다 멀리있는 건 제외
/// </summary>
public class CoverLookUp : MonoBehaviour
{
    private List<Vector3[]> allCoverSpots; //모든 장애물 배열
    private GameObject[] covers;
    private List<int> coverHashCodes;//cover unity ID;
    private Dictionary<float, Vector3> filteredSpots;//멀거나 특정 각도 밖인 것들 필터링. 필터링된 지점들을 모아둠

    private GameObject[] GetObjectsInLayerMask(int layerMask)
    {
        List<GameObject> ret = new List<GameObject>();


        //현재 씬에 있는 오브젝트 중 특정 레이어인 오브젝트를 리스트에 모음
        foreach(GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if(go.activeInHierarchy && layerMask == (layerMask | (1 << go.layer)))
            {
                ret.Add(go);
            }
        }

        return ret.ToArray();
    }

    private void ProcessPoint(List<Vector3> vector3s, Vector3 nativePoint, float range)
    {
        NavMeshHit hit;

        //현재 위치에서 특정 포인트 찍어서 나에게 유효한지 아닌지
        //메시 콜라이더에서 특정 포인트 찍어서 갈수 있는 지점 찾음(장애물에서)
        if(NavMesh.SamplePosition(nativePoint, out hit, range, NavMesh.AllAreas))
        {
            vector3s.Add(hit.position);
        }
    }

    private Vector3[] GetSpots(GameObject go, LayerMask obstacleMask)
    {
        List<Vector3> bounds = new List<Vector3>();

        foreach (Collider col in go.GetComponents<Collider>()) 
        {
            float baseHeight = (col.bounds.center - col.bounds.extents).y;
            float range = 2 * col.bounds.extents.y;

            //현재 게임오브젝트 스케일된 값에 따라 벡터 구함
            Vector3 deslocalForward = go.transform.forward * go.transform.localScale.z * 0.5f; 
            Vector3 deslocalRight = go.transform.right * go.transform.localScale.x * 0.5f;

            if (go.GetComponent<MeshCollider>())
            {
                //갈 수 있는 위치 샘플링
                float maxBounds = go.GetComponent<MeshCollider>().bounds.extents.z + go.GetComponent <MeshCollider>().bounds.extents.x;
                Vector3 originForward = col.bounds.center + go.transform.forward * maxBounds;
                Vector3 originRight = col.bounds.center + go.transform.right * maxBounds;


                //메시 콜라이더 중심에 따라 모양 다름, 그거에 대한 벡터를 스케일에 따라 구함(레이를 이용해 총 크기 구함)
                if(Physics.Raycast(originForward, col.bounds.center - originForward, out RaycastHit hit, maxBounds, obstacleMask))
                {
                    deslocalForward = hit.point - col.bounds.center;
                }
                if(Physics.Raycast(originRight, col.bounds.center - originRight, out hit, maxBounds, obstacleMask))
                {
                    deslocalRight = hit.point - col.bounds.center;
                }
            }

            //scale이 1,1,1-> 바로 bounds 크기만큼 곱해서 총 크기 구함
            else if(Vector3.Equals(go.transform.localScale, Vector3.one))
            {
                deslocalForward = go.transform.forward * col.bounds.extents.z;
                deslocalRight = go.transform.right * col.bounds.extents.x;
            }

            //12개 점 찍어서 유효한 지점이 있는지 확인
            float edgeFactor = 0.75f;
            ProcessPoint(bounds, col.bounds.center + deslocalRight + deslocalForward * edgeFactor, range);///우상단
            ProcessPoint(bounds, col.bounds.center + deslocalForward + deslocalRight * edgeFactor, range);
            ProcessPoint(bounds, col.bounds.center + deslocalForward, range); //중심에서 앞에
            ProcessPoint(bounds, col.bounds.center + deslocalForward - deslocalRight * edgeFactor, range);
            ProcessPoint(bounds, col.bounds.center - deslocalRight + deslocalForward * edgeFactor, range); //반대편
            ProcessPoint(bounds, col.bounds.center + deslocalRight, range); //오른쪽
            ProcessPoint(bounds, col.bounds.center + deslocalRight - deslocalForward * edgeFactor, range); //오른쪽에서 뒤로
            ProcessPoint(bounds, col.bounds.center - deslocalForward + deslocalRight * edgeFactor, range); 
            ProcessPoint(bounds, col.bounds.center - deslocalForward, range); //뒤
            ProcessPoint(bounds, col.bounds.center - deslocalForward - deslocalRight * edgeFactor, range); //뒤 왼쪽
            ProcessPoint(bounds, col.bounds.center - deslocalRight - deslocalForward * edgeFactor, range); //오른쪽 뒤
            ProcessPoint(bounds, col.bounds.center - deslocalRight, range);
        }

        return bounds.ToArray();
    }

    public void Setup(LayerMask coverMask)
    {
        covers = GetObjectsInLayerMask(coverMask);
        coverHashCodes = new List<int>();
        allCoverSpots = new List<Vector3[]>();

        foreach (GameObject cover in covers)
        {

            //장애물에서 갈 수 있는 위치
            allCoverSpots.Add(GetSpots(cover, coverMask));

            coverHashCodes.Add(cover.GetHashCode());
        }
    }

    //목표물이 경로에 있는지 확인, 대상이 각도 안에 있고 지점보다(spot) 가까이 있니냐
    //필터링할때 쓰임
    private bool TargetInPath(Vector3 origin, Vector3 spot, Vector3 target, float angle)
    {
        Vector3 dirToTarget = (target - origin).normalized;
        Vector3 dirToSpot = (spot - origin).normalized;

        if(Vector3.Angle(dirToSpot, dirToTarget) <= angle)
        {
            float targetDist = (target - origin).sqrMagnitude;//Vector3 distance 대신 많이 씀
            float spotDist = (spot - origin).sqrMagnitude;
            return (targetDist <= spotDist);
        }
        return false;
    }

    //가장 가까운 유효한 지점을 찾아줌, 거리도 같이 줌
    private ArrayList FilterSpots(StateController controller)
    {
        float minDist = Mathf.Infinity;
        filteredSpots = new Dictionary<float, Vector3>();
        int nextCoverHash = -1;
        for(int i=0; i<allCoverSpots.Count; i++)
        {
            //이미 찾았거나 activeself 꺼져있으면 필터링 안함
            if(!covers[i].activeSelf || coverHashCodes[i] == controller.coverHash)
            {
                continue;
            }
            foreach(Vector3 spot in allCoverSpots[i])
            {
                Vector3 vectorDist = controller.personalTarget - spot;
                float searchDist = (controller.transform.position - spot).sqrMagnitude;

                //보이는 것보다작거나, 영역안에 있냐
                if(vectorDist.sqrMagnitude <= controller.viewRadius*controller.viewRadius &&
                    Physics.Raycast(spot,vectorDist, out RaycastHit hit, vectorDist.sqrMagnitude, controller.generalStats.coverMask))
                {
                    //플레이어가 npc와 스팟 사이에 있지 않은지 확인하고, 보이는 각도의 1/4각을 사용
                    //타겟보다 멀리있는건 거른다
                    if(hit.collider == covers[i].GetComponent<Collider>() && !TargetInPath(controller.transform.position, spot, controller.personalTarget, controller.viewAngle / 4))
                    {
                        if (!filteredSpots.ContainsKey(searchDist))
                        {
                            filteredSpots.Add(searchDist, spot);
                        }
                        else
                        {
                            continue;
                        }

                        //최소거리 갱신
                        if (minDist > searchDist)
                        {
                            minDist = searchDist;
                            nextCoverHash = coverHashCodes[i];
                        }
                    }
                }
            }
        }
        ArrayList returnArray = new ArrayList();
        returnArray.Add(nextCoverHash);
        returnArray.Add(minDist);
        return returnArray;
    }

    public ArrayList GetBestCoverSpot(StateController controller)
    {
        ArrayList nextCoverData = FilterSpots(controller);
        int nextCoverHash = (int)nextCoverData[0];
        float minDist = (float)nextCoverData[1];

        ArrayList returnArray = new ArrayList();

        //필터에 걸린게 없음
        if(filteredSpots.Count == 0)
        {
            returnArray.Add(-1);
            returnArray.Add(Vector3.positiveInfinity);
        }
        //있음
        else
        {
            returnArray.Add(nextCoverHash);
            returnArray.Add(filteredSpots[minDist]);
        }

        return returnArray;
    }
}
