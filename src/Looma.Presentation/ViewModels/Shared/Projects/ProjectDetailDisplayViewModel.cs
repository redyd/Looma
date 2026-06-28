// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Shared.Projects;

public partial class ProjectDetailDisplayViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int ProjectId { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Status Status { get; set; }

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial DateOnly? BeginDate { get; set; }

    [ObservableProperty]
    public partial DateOnly? EndDate { get; set; }

    [ObservableProperty]
    public partial Pattern? Pattern { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ProjectWoolUsageViewModel> Wools { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProjectImageViewModel> Images { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedImageIndex { get; set; } = -1;

    public string StatusDisplay => Translation[$"Enum_{Status}"];
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? Translation["Common_NoNote"] : Note!;
    public string PatternName => Pattern?.Name ?? Translation["Projects_NoPattern"];
    public string PatternTypeDisplay => Pattern?.Type.GetDisplayName() ?? "-";
    public string PatternNoteDisplay => string.IsNullOrWhiteSpace(Pattern?.Note) ? Translation["Common_NoNote"] : Pattern.Note!;
    public string PatternActionText => Pattern is null ? Translation["Common_Add"] : Translation["Common_Open"];
    public bool HasWools => Wools.Count > 0;
    public bool HasImages => Images.Count > 0;
    public bool HasMultipleImages => Images.Count > 1;
    public bool IsWishlist => Status == Status.Wishlist;
    public bool IsInProgress => Status == Status.InProgress;
    public bool IsPaused => Status == Status.Paused;
    public bool HasProjectActions => Status != Status.Finished;
    public ProjectImageViewModel? SelectedImage =>
        SelectedImageIndex >= 0 && SelectedImageIndex < Images.Count ? Images[SelectedImageIndex] : null;
    public string ImagePositionDisplay => HasImages ? $"{SelectedImageIndex + 1} / {Images.Count}" : string.Empty;

    public IList<StatItem> PatternStats =>
    [
        new() { Label = Translation["Navigation_Documents"], Value = (Pattern?.Documents.Count ?? 0).ToString("N0"), Unit = "x", IsFirst = true },
        new() { Label = Translation["PatternsDetail_LinkedProjects"], Value = (Pattern?.Projects.Count ?? 0).ToString("N0"), Unit = "x" }
    ];

    public IList<InfoItem> ProjectInfos =>
    [
        new() { Label = Translation["Common_Name"], Value = Name },
        new() { Label = Translation["Common_Status"], Value = StatusDisplay },
        new() { Label = Translation["Common_Begin"], Value = FormatDate(BeginDate) },
        new() { Label = Translation["Common_End"], Value = FormatDate(EndDate) },
        new() { Label = Translation["Common_Wools"], Value = Wools.Count.ToString("N0") },
    ];

    public IList<InfoItem> PatternInfos =>
    [
        new() { Label = Translation["Common_Pattern"], Value = PatternName },
        new() { Label = Translation["Common_Type"], Value = PatternTypeDisplay },
        new() { Label = Translation["Common_Origin"], Value = Pattern?.IsPersonal == true ? Translation["Common_Personal"] : Translation["Common_NotPersonal"] },
        new() { Label = Translation["Common_Link"], Value = Pattern?.Url ?? Translation["Common_None"] },
        new() { Label = Translation["Common_Begin"], Value = FormatDate(Pattern?.BeginDate) },
        new() { Label = Translation["Common_End"], Value = FormatDate(Pattern?.EndDate) },
    ];

    public void RefreshProject(Project project)
    {
        ProjectId = project.ProjectId;
        Name = project.Name;
        Status = project.Status;
        Note = project.Note;
        BeginDate = project.BeginDate;
        EndDate = project.EndDate;
        Pattern = project.Pattern;

        RefreshProjectProperties();
        RefreshPatternProperties();
    }

    public void RefreshWools(ObservableCollection<ProjectWoolUsageViewModel> wools)
    {
        Wools = wools;
        OnPropertyChanged(nameof(HasWools));
        OnPropertyChanged(nameof(ProjectInfos));
    }

    public void RefreshImages(ObservableCollection<ProjectImageViewModel> images)
    {
        Images = images;
        SelectedImageIndex = Images.Count == 0 ? -1 : Math.Clamp(SelectedImageIndex, 0, Images.Count - 1);
        RefreshImageProperties();
    }

    public void RefreshTranslations()
    {
        RefreshProjectProperties();
        RefreshPatternProperties();

        foreach (var wool in Wools)
            wool.RefreshTranslations();
    }

    partial void OnImagesChanged(ObservableCollection<ProjectImageViewModel> value) => RefreshImageProperties();
    partial void OnSelectedImageIndexChanged(int value) => RefreshImageProperties();
    partial void OnNoteChanged(string? value) => OnPropertyChanged(nameof(NoteDisplay));

    private void RefreshProjectProperties()
    {
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(NoteDisplay));
        OnPropertyChanged(nameof(IsWishlist));
        OnPropertyChanged(nameof(IsInProgress));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(HasProjectActions));
        OnPropertyChanged(nameof(ProjectInfos));
    }

    private void RefreshPatternProperties()
    {
        OnPropertyChanged(nameof(PatternName));
        OnPropertyChanged(nameof(PatternTypeDisplay));
        OnPropertyChanged(nameof(PatternNoteDisplay));
        OnPropertyChanged(nameof(PatternActionText));
        OnPropertyChanged(nameof(PatternInfos));
        OnPropertyChanged(nameof(PatternStats));
    }

    private void RefreshImageProperties()
    {
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(SelectedImage));
        OnPropertyChanged(nameof(ImagePositionDisplay));
    }

    private string FormatDate(DateOnly? value) =>
        value is null ? Translation["Common_NoneFeminine"] : value.Value.ToString("dd/MM/yyyy");
}
