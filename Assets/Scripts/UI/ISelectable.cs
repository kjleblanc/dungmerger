namespace MergeDungeon.Core
{
    public interface ISelectable
    {
        // Called when this becomes selected (first tap)
        void OnSelectTap();
        // Called when tapped again while already selected
        void OnActivateTap();
    }
}

