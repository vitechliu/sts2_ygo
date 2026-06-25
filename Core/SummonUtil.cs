using VYgo.Core.Cards;
using VYgo.Core.Effects;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Core;

public static class SummonUtil {
    
    //连接召唤完整函数
    public static async Task linkSummon(CoreCard coreCard) {
        int linkMarkers = coreCard.Def.Value;
        int linkCount = coreCard.LinkCount.Value;


        NLinkSummonManager manager;

        List<int> trailAnim1;
        (trailAnim1, linkMarkers) = resolveLink(linkMarkers, linkCount > 5 ? 2 : 1);

        if (linkCount > 1) {
            List<int> trailAnim2;
            (trailAnim2, linkMarkers) = resolveLink(linkMarkers, linkCount > 4 ? 2 : 1);
        }
        if (linkCount > 2) {
            List<int> trailAnim3;
            (trailAnim3, linkMarkers) = resolveLink(linkMarkers, linkCount > 3 ? 2 : 1);
        }

    }

    static (List<int>, int) resolveLink(int linkMarkers, int need) {
        int foundMarker = 0;
        int foundMarkerCount = 0;
        List<int> res = [];
        foreach (var (trail, marker) in markers) {
            if (foundMarkerCount < need && (linkMarkers & (int)marker) > 0) {
                foundMarkerCount++;
                foundMarker += (int)marker;
                res.Add(trail);
            }
        }
        return (res, linkMarkers - foundMarker);
    }

    private static readonly List<(int, CardLinkMarker)> markers = new() {
        (2, CardLinkMarker.Top),
        (1, CardLinkMarker.TopLeft),
        (4, CardLinkMarker.Left),
        (6, CardLinkMarker.BottomLeft),
        (7, CardLinkMarker.Bottom),
        (8, CardLinkMarker.BottomRight),
        (5, CardLinkMarker.Right),
        (3, CardLinkMarker.TopRight),
    };
}