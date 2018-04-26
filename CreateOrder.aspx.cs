using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace warehouse
{
    public partial class CreateOrder : System.Web.UI.Page
    {
        private DataTable suppliers
        {
            get
            {
                return Login.db.get_suppliers();
            }
        }

        private DataTable components_data
        {
            get
            {
                return Login.db.get_components_data();
            }
        }

        private DataTable supplier_bind
        {
            get
            {
                return Login.db.get_supplier_bind();
            }
        }

        private DataTable allItems
        {
            get
            {
                return Login.db.load_items();
            }
        }

        private DataTable componentsTable
        {
            get
            {
                if (Session["CreateOrderComponentsTable"] != null)
                {
                    return Session["CreateOrderComponentsTable"] as DataTable;
                }
                else
                {
                    DataTable ord = new DataTable();
                    ord.Columns.AddRange(new DataColumn[3] { new DataColumn("articula"), new DataColumn("name"), new DataColumn("unit") });
                    Session["CreateOrderComponentsTable"] = ord;
                    return Session["CreateOrderComponentsTable"] as DataTable;
                }
            }
            set { Session["CreateOrderComponentsTable"] = value; }
        }

        private DataTable selectedTable
        {
            get
            {
                if (Session["CreateOrderSelectedTable"] != null)
                {
                    return Session["CreateOrderSelectedTable"] as DataTable;
                }
                else
                {
                    DataTable ord = new DataTable();
                    ord.Columns.AddRange(new DataColumn[4] { new DataColumn("articula"), new DataColumn("name"), new DataColumn("amount"), new DataColumn("unit") });
                    Session["CreateOrderSelectedTable"] = ord;
                    return Session["CreateOrderSelectedTable"] as DataTable;
                }
            }
            set { Session["CreateOrderSelectedTable"] = value; }
        }

        private Dictionary<string, float> quantitytable
        {
            get { return Session["CreateOrderAmount"] == null ? new Dictionary<string, float>() : Session["CreateOrderAmount"] as Dictionary<string, float>; }
            set { Session["CreateOrderAmount"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (!Login.grant_access(HttpContext.Current.Request.Url.AbsolutePath, User.Identity.Name))
                {
                    Response.Redirect("~/AccessDenied.aspx", true);
                }
                if (!IsPostBack)
                {
                    initialize();
                }
            }
            else
            {
                Session["previousPage"] = HttpContext.Current.Request.Url.AbsolutePath;
                Response.Redirect("~/Account/Login.aspx", true);
            }
        }

        private void initialize()
        {
            saveCheckbox.Checked = false;
            clearbutton.Attributes["onclick"] = "if(!confirm('Do you want to delete all of the items?')){ return false; };";
            createOrderButton.Attributes["onclick"] = "if(!confirm('Are you sure to create this order?')){ return false; };";
            componentsTable.Clear();
            selectedTable.Clear();
            ItemGridViewDataBind();
            OrderGridViewDataBind();
            load_data();
        }


        private void load_data()
        {
            supplierlist.Items.Clear();
            if(searchbox.Text == "")
                supplierlist.Items.Add("-выбрать поставщика-");
            foreach(DataRow row in suppliers.Rows)
            {
                string name = row["name"].ToString();
                string id = row["id"].ToString();
                if (name.ToLower().Contains(searchbox.Text.ToLower()))
                {
                    ListItem item = new ListItem(name);
                    item.Value = id;
                    supplierlist.Items.Add(item);
                }
            }

            componentlist.Items.Clear();
            foreach(DataRow row in allItems.Rows)
            {
                string articula = row["articula"].ToString();
                string name = row["name"].ToString();
                if (name.ToLower().Contains(filterbox.Text.ToLower()) || articula.StartsWith(filterbox.Text))
                {
                    ListItem item = new ListItem(articula + " - " + name);
                    item.Value = articula;
                    componentlist.Items.Add(item);
                }
            }
        }

        protected void filterbox_TextChanged(object sender, EventArgs e)
        {
            load_data();
        }

        protected void supplierlist_SelectedIndexChanged(object sender, EventArgs e)
        {
            int sid;
            saveCheckbox.Checked = false;
            if (!int.TryParse(supplierlist.SelectedValue, out sid))
            {
                supplierLabel.Text = "";
                addcomponentpanel.Enabled = false;
                componentsTable.Clear();
                ItemGridViewDataBind();
                return;
            }
            addcomponentpanel.Enabled = true;
            supplierLabel.Text = supplierlist.Items[supplierlist.SelectedIndex].ToString();
            DataRow[] components = supplier_bind.Select("sid = " + sid);
            componentsTable.Rows.Clear();
            foreach(DataRow row in components)
            {
                string articula = row["articula"].ToString();
                string name = row["name"].ToString();
                string unit = row["unit"].ToString();
                componentsTable.Rows.Add(articula, name, unit);
            }
            ItemGridViewDataBind();
        }

        protected void addItembutton_Click(object sender, EventArgs e)
        {
            string articula = componentlist.SelectedValue;
            foreach(DataRow row in componentsTable.Rows)
            {
                string item = row["articula"].ToString();
                if (item.Equals(articula))
                    return;
            }

            DataRow match = allItems.AsEnumerable().SingleOrDefault(rt => rt.Field<string>("articula").CompareTo(articula) == 0 ? true : false);
            if (match == null)
                return;

            componentsTable.Rows.Add(match["articula"].ToString(), match["name"].ToString(), match["unit"].ToString());
            ItemGridViewDataBind();
        }

        protected void ItemGridView_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "add")
            {
                Dictionary<string, float> qtable;
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = ItemGridView.Rows[index];
                TextBox box = row.Cells[2].FindControl("orderbox") as TextBox;
                string articula = row.Cells[0].Text;
                string name = row.Cells[1].Text;
                string unit = row.Cells[3].Text;
                float quantity;
                ////////////////////////////////////////////////////////////////////
                if (!float.TryParse(box.Text, out quantity))
                    return;
                if (quantity == 0)
                    return;
                qtable = quantitytable;
                qtable[articula] = quantity;
                quantitytable = qtable;
                DataRow[] match = selectedTable.Select("articula = " + articula);
                if (match.Length == 0)
                {
                    selectedTable.Rows.Add(articula, name, quantity, unit);
                }
                else
                {
                    match[0][2] = quantity;
                }

                ItemGridViewDataBind();
                OrderGridViewDataBind();

            }
        }

        private void ItemGridViewDataBind()
        {
            ItemGridView.DataSource = componentsTable;
            ItemGridView.DataBind();
        }

        private void OrderGridViewDataBind()
        {
            if (selectedTable.Rows.Count == 0)
            {
                supplierlist.Enabled = true;
                clearbutton.Visible = false;
                createOrderButton.Enabled = false;
            }

            else
            {
                supplierlist.Enabled = false;
                clearbutton.Visible = true;
                createOrderButton.Enabled = true;
            }
            OrderGridView.DataSource = selectedTable;
            OrderGridView.DataBind();
        }

        protected void ItemGridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string articula = e.Row.Cells[0].Text;
                TextBox box = e.Row.Cells[2].FindControl("orderbox") as TextBox;
                if(quantitytable.ContainsKey(articula))
                {
                    if (quantitytable[articula] > 0)
                    {
                        box.Text = quantitytable[articula].ToString();
                        e.Row.BackColor = System.Drawing.Color.SkyBlue;
                    }
                    else
                        box.Text = "0";
                }
                else
                    box.Text = "0";
            }
        }

        protected void OrderGridView_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int index = e.RowIndex;
            DataRow row = selectedTable.Rows[index];
            string articula = row["articula"].ToString();
            quantitytable.Remove(articula);
            row.Delete();

            ItemGridViewDataBind();
            OrderGridViewDataBind();
        }

        protected void clearbutton_Click(object sender, EventArgs e)
        {
            quantitytable.Clear();
            selectedTable.Clear();
            ItemGridViewDataBind();
            OrderGridViewDataBind();
        }

        protected void createOrderButton_Click(object sender, EventArgs e)
        {
            if (Login.db.Add_new_order(selectedTable, User.Identity.Name, supplierlist.SelectedValue))
            {
                if(saveCheckbox.Checked)
                {
                    AddNewList(supplierlist.SelectedValue);
                }

                clearbutton_Click(null, null);
                Session["transactions"] = null;
                Session["message"] = "транзакция прошла успешно";
                Response.Redirect("~/Message.aspx", true);
            }

        }

        private void AddNewList(string sid)
        {
            int id;
            if (!int.TryParse(sid, out id))
                return;
            foreach(DataRow row in selectedTable.Rows)
            {
                string articula = row["articula"].ToString();
                Login.db.add_component_to_supplier(articula, id);
            }
        }

        protected void itemsviewpanel_Load(object sender, EventArgs e)
        {
            string script = "window.onload = function() { RestoreScrollPosition(); };";
            ClientScript.RegisterStartupScript(this.GetType(), "RestorePosition", script, true);
        }

        protected void orderpanel_Load(object sender, EventArgs e)
        {

        }
    }
}