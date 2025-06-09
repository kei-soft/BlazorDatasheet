using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells
{
    public partial class CellStore
    {
        /// <summary>
        /// Fired when cells are merged
        /// </summary>
        public event EventHandler<IRegion>? RegionMerged;

        /// <summary>
        /// Fired when cells are un-merged
        /// </summary>
        public event EventHandler<IRegion>? RegionUnMerged;

        /// <summary>
        /// The merged cells in the sheet.
        /// </summary>
        private readonly RegionDataStore<bool> _mergeStore = new(1, expandWhenInsertAfter: false);

        internal RegionDataStore<bool> GetMergeStore() => _mergeStore;

        /// <summary>
        /// Add a range as a merged cell. If the range overlaps any existing merged cells, the merge
        /// will not happen.
        /// </summary>
        /// <param name="region"></param>
        public void Merge(IRegion region)
        {
            var merge = new MergeCellsCommand(region);
            Sheet.Commands.ExecuteCommand(merge);
        }

        /// <summary>
        /// Adds regions as a merged cell. If the range overlaps any existing merged cells, the merge
        /// will not happen.
        /// </summary>
        /// <param name="regions"></param>
        public void Merge(IEnumerable<IRegion> regions)
        {
            Sheet.Commands.BeginCommandGroup();
            foreach (var region in regions)
            {
                Sheet.Commands.ExecuteCommand(new MergeCellsCommand(region));
            }

            Sheet.Commands.EndCommandGroup();
        }

        internal bool MergeImpl(IRegion region)
        {
            _mergeStore.Add(region, true);
            RegionMerged?.Invoke(this, region);
            Sheet.MarkDirty(region);
            return true;
        }

        /// <summary>
        /// Un-merge selected regions of cells.
        /// </summary>
        /// <param name="regions"></param>
        public void UnMerge(IEnumerable<IRegion> regions)
        {
            Sheet.Commands.BeginCommandGroup();

            foreach (var region in regions)
            {
                Sheet.Commands.ExecuteCommand(new UnMergeCellsCommand(region));
            }

            Sheet.Commands.EndCommandGroup();
        }

        /// <summary>
        /// Un-merge all cells that overlap the range
        /// </summary>
        /// <param name="region"></param>
        internal void UnMergeCellsImpl(IRegion region)
        {
            var mergedCellsInRange = _mergeStore.GetDataRegions(region).ToList();
            var updateRegion = mergedCellsInRange.FirstOrDefault()?.Region;
            foreach (var merge in mergedCellsInRange)
            {
                updateRegion = region.GetBoundingRegion(updateRegion);
                _mergeStore.Delete(merge);
                RegionUnMerged?.Invoke(this, region);
            }

            if (updateRegion != null)
                Sheet.MarkDirty(updateRegion);
        }

        /// <summary>
        /// Un-merge all cells that overlap the range
        /// </summary>
        /// <param name="region"></param>
        internal void UnMergeCellsImpl(SheetRange range)
        {
            UnMergeCellsImpl(range.Region);
        }

        /// <summary>
        /// Returns whether the position is inside a merged cell
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool IsInsideMerge(int row, int col)
        {
            return GetMerge(row, col) != null;
        }

        /// <summary>
        /// Returns the region (if any) that exists at the given position.
        /// There will only be one region at most, because merges cannot overlap.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public IRegion? GetMerge(int row, int col)
        {
            var merges = _mergeStore.GetDataRegions(row, col).ToList();
            // There will only be one merge because we don't allow overlapping
            return merges.Any() ? merges[0].Region : null;
        }

        /// <summary>
        /// Returns all merged regions overlapping a region.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public IEnumerable<IRegion> GetMerges(IRegion region)
        {
            return _mergeStore.GetDataRegions(region).Select(x => x.Region);
        }

        /// <summary>
        /// Returns all merged regions overlapping a list of regions.
        /// </summary>
        /// <param name="regions"></param>
        /// <returns></returns>
        public IEnumerable<IRegion> GetMerges(IEnumerable<IRegion> regions)
        {
            return _mergeStore.GetDataRegions(regions).Select(x => x.Region);
        }

        /// <summary>
        /// Returns whether the sheet has any merged cells.
        /// </summary>
        /// <returns></returns>
        public bool AnyMerges()
        {
            return _mergeStore.Any();
        }

        /// <summary>
        /// Returns whether the region has any merged cells.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool AnyMerges(IRegion region)
        {
            return _mergeStore.Any(region);
        }
    }
}