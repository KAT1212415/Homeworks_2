using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClassLibrary10.Abstractions;
using System.Threading.Tasks;
using static ClassLibrary10.Abstractions.ISectionService;

namespace ClassLibrary10.Viewmodels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly ISelectionService _selectionService;
        private readonly ISectionService _sectionService;
        private readonly RevitTask _revitTask;
        private string _sectionName = "Разрез";
        private double _widthOffsetMm = 100;
        private double _depthOffsetMm = 100;
        private double _heightOffsetMm = 100;
        XYZ viewDirection = null; 
        XYZ verticalDirection = null;

        public MainWindowViewModel(
            ISelectionService selectionService,
            ISectionService sectionService,
            RevitTask revitTask
            )
        {
            CreateSectionCommand = new AsyncRelayCommand(OnCreateSectionCommandExecute);
            _selectionService = selectionService;
            _sectionService = sectionService;
            _revitTask = revitTask;
        }

        public string SectionName
        {
            get => _sectionName;
            set => SetProperty(ref _sectionName, value);
        }

        public double WidthOffsetMm
        {
            get => _widthOffsetMm;
            set => SetProperty(ref _widthOffsetMm, value);
        }

        public double DepthOffsetMm
        {
            get => _depthOffsetMm;
            set => SetProperty(ref _depthOffsetMm, value);
        }

        public double HeightOffsetMm
        {
            get => _heightOffsetMm;
            set => SetProperty(ref _heightOffsetMm, value);
        }

        public AsyncRelayCommand CreateSectionCommand { get; }

        private async Task OnCreateSectionCommandExecute()
        {
            FamilyInstance familyInstance = _selectionService.PickFamilyInstance();
            if (familyInstance == null)
            {
                return;
            }

            var orientations = new[]
    {
        ("Восток", XYZ.BasisY, XYZ.BasisX),
        ("План",  XYZ.BasisY, XYZ.BasisZ),
        ("Север",   -XYZ.BasisZ, XYZ.BasisY)
    };

            foreach (var (name, view, vert) in orientations)
            {
                bool isCreated = await _revitTask.Run<bool>(app =>
                    _sectionService.CreateSection(
                        familyInstance,
                        WidthOffsetMm,
                        DepthOffsetMm,
                        HeightOffsetMm,
                        $"{SectionName}_{name}",
                        view,
                        vert));

                if (!isCreated)
                {
                    TaskDialog.Show("Ошибка", "Что-то пошло не так");
                }
                //else
                //{
                //    TaskDialog.Show("Успех", "Все пошло так");
                //}

            }
            TaskDialog.Show("Успех", "Разрезы созданы");

            //bool isCreated = await _revitTask.Run<bool>(app =>
            //_sectionService.CreateSection(
            //    familyInstance,
            //    WidthOffsetMm,
            //    DepthOffsetMm,
            //    HeightOffsetMm,
            //    SectionName,
            //    viewDirection,
            //    verticalDirection
            //     ));


        }
    }
}
