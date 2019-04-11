using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Comparers;
using Sitecore.Data.Items;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using Sitecore.XA.Foundation.Multisite.Pipelines.PushCloneChanges;
using Sitecore.XA.Foundation.Multisite.Services;

namespace Sitecore.Support.XA.Foundation.Multisite.Services
{
  public class PushCloneService : IPushCloneService
  {
    private readonly IPushCloneCoordinatorService _coordinatorService;

    public PushCloneService(IPushCloneCoordinatorService pushCloneCoordinatorService)
    {
      _coordinatorService = pushCloneCoordinatorService;
    }

    public void AddChild(Item item)
    {
      foreach (var current in item.Parent.GetClones())
      {
        if (_coordinatorService.ShouldProcess(current) && item.Versions.GetVersionNumbers().Length != 0)
        {
          var item2 = item.CloneTo(current);
          ProtectItem(item2);
        }
      }
    }

    public void Move(Item item)
    {
      if (item.Parent.HasClones)
      {
        foreach (var current in GetCloneItem(item.Parent).ToList())
        {
          if (!_coordinatorService.ShouldProcess(current))
          {
            break;
          }

          foreach (var current2 in GetCloneItem(item))
          {
            if (current.Paths.FullPath.Contains(current2.Paths.FullPath) ||
                current2.Paths.FullPath.Contains(current.Paths.FullPath))
            {
              current2.MoveTo(current);
            }
          }
        }
      }
    }

    public void Remove(Item item)
    {
      foreach (var current in item.GetClones())
      {
        if (_coordinatorService.ShouldProcess(current))
        {
          current.Delete();
        }
      }
    }

    public void SaveClone(Item item, ItemChanges changes)
    {
      foreach (var current in GetCloneItem(item))
      {
        if (!_coordinatorService.ShouldProcess(current))
        {
          break;
        }

        var args = new PushCloneChangesArgs
        {
          Item = item,
          Changes = changes,
          Clone = current
        };
        CorePipeline.Run("pushCloneChanges", args);
      }
    }

    public void AddVersion(Item item)
    {
      var parent = item.Parent;
      var latestVersion = item.Versions.GetLatestVersion();
      var uri = latestVersion.Uri;
      var cloneItem = GetCloneItem(latestVersion);
      var list = cloneItem as IList<Item> ?? cloneItem.ToList();
      if (!list.Any() && parent.HasClones)
      {
        using (var enumerator = GetCloneItem(parent).GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var current = enumerator.Current;
            if (_coordinatorService.ShouldProcess(current))
            {
              var item2 = item.CloneTo(current);
              CopyWorkflow(item, item2);
              ProtectItem(item2);
            }
          }

          return;
        }
      }

      foreach (var current2 in list)
      {
        if (!_coordinatorService.ShouldProcess(current2))
        {
          break;
        }

        var newItemVersion = current2.Database.GetItem(current2.ID, latestVersion.Language).Versions.AddVersion();
        newItemVersion.Editing.BeginEdit();
        newItemVersion[FieldIDs.Source] = uri.ToString();
        newItemVersion[FieldIDs.SourceItem] = uri.ToString(false);
        newItemVersion.Editing.EndEdit();
      }
    }

    public void RemoveVersion(Item commandItem)
    {
    }

    protected virtual void CopyWorkflow(Item source, Item target)
    {
      var item = source.Database.GetItem(source.ID);
      target.Editing.BeginEdit();
      target[FieldIDs.Workflow] = item[FieldIDs.Workflow];
      target[FieldIDs.WorkflowState] = item[FieldIDs.WorkflowState];
      target.Editing.EndEdit();
    }

    protected virtual void ProtectItem(Item item)
    {
      item.Editing.BeginEdit();
      item.Appearance.ReadOnly = true;
      item.Editing.EndEdit();
    }

    protected virtual IEnumerable<Item> GetCloneItem(Item item)
    {
      return item.GetClones().Distinct(new ItemIdComparer());
    }
  }
}