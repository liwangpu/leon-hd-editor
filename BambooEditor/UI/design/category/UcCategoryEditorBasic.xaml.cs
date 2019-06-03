using BambooUploader.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BambooEditor.UI.design.category
{
    /// <summary>
    /// Interaction logic for UcCategoryEditorBase.xaml
    /// </summary>
    public partial class UcCategoryEditorBasic : UserControl, ICategoryEditor
    {
        public UcCategoryEditorBasic()
        {
            InitializeComponent();
        }

        CategoryModel target;
        public void SetTarget(CategoryModel Target)
        {
            target = Target;
            if(target == null)
            {
                txtId.Text = "";
                txtName.Text = "";
                txtType.Text = "";
                txtIcon.Text = "";
                txtDescription.Text = "";
                txtDisplayIndex.Text = "";
                chkActivated.IsChecked = false;
                txtParentId.Text = "";
                return;
            }
            txtId.Text = target.id;
            txtName.Text = target.name;
            txtType.Text = target.type;
            txtIcon.Text = target.icon;
            txtDescription.Text = target.description;
            txtDisplayIndex.Text = target.displayIndex.ToString();
            chkActivated.IsChecked = target.activeFlag != 0;
            txtParentId.Text = target.parentId;
        }

        public void SaveData()
        {
            if (target == null)
                return;

            target.name = txtName.Text;
            target.type = txtType.Text;
            target.icon = txtIcon.Text;
            target.description = txtDescription.Text;
            int index = 0;
            int.TryParse(txtDisplayIndex.Text, out index);
            target.displayIndex = index;
            target.activeFlag = chkActivated.IsChecked == true ? 1 : 0;
            target.parentId = txtParentId.Text;
        }

    }
}
