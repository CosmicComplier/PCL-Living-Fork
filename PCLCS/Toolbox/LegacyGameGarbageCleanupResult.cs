// Behavior source: LF Modules/Base tooling helpers; repository LICENCE.
// LF lineage f4aa0fae62c3ee8af24c81e61844c8fdd13c8983; upstream PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
// Existing LF policies are preserved; no new native/performance/MMC behavior.
namespace PCLCS;
public class LegacyGameGarbageCleanupResult
{
    public int DeletedFiles { get; set; }
    public long DeletedBytes { get; set; }
    public int SkippedItems { get; set; }
}
