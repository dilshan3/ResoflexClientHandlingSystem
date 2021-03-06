﻿using MySql.Data.MySqlClient;
using ResoflexClientHandlingSystem.Core;
using ResoflexClientHandlingSystem.Role;
using ResoflexClientHandlingSystem.ScheduleForms;
using ResoflexClientHandlingSystem.ScheduleForms.Reports;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResoflexClientHandlingSystem
{
    public partial class ScheduleForm : MetroFramework.Forms.MetroForm
    {

        public ScheduleForm()
        {
            InitializeComponent();

            scheduleGrid.DataSource = getSchedules();

            scheduleGrid.Columns[1].Visible = false;
            scheduleGrid.Columns[2].Visible = false;

            //autocomplete data source
            projectName.AutoCompleteCustomSource = projectNameAutoComplete();
            clientName.AutoCompleteCustomSource = clientNameAutoComplete();

            //tiles initilization
            totalScheduleTile();
            incompleteScheduleTile();
        }

        private void ScheduleForm_Load(object sender, EventArgs e)
        {
            
        }

        //buttons
        private void deleteSchedule_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = scheduleGrid.CurrentRow;

            Schedule schedule = new Schedule();
            schedule.ScheduleId = int.Parse(dr.Cells[0].Value.ToString());
            schedule.ProjectOfSchedule = new Project(int.Parse(dr.Cells[1].Value.ToString()));

            DialogResult res = MessageBox.Show("Are you sure you want delete this schedule?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                if (Database.deleteSchedule(schedule))
                {
                    MessageBox.Show("Schedule Successfully Deleted\n", "Schedule Deleted", MessageBoxButtons.OK);

                    scheduleGrid.DataSource = getSchedules();
                }
                else
                {
                    MessageBox.Show("Something went wrong!\n", "Schedule Deleted", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void updateScheduleGrid()
        {
            scheduleGrid.DataSource = getSchedules();

        }

        private void addSchedule_Click(object sender, EventArgs e)
        {
            AddScheduleForm asf = new AddScheduleForm();
            asf.ShowDialog();

            scheduleGrid.DataSource = getSchedules();
        }

        private void schHome_Click(object sender, EventArgs e)
        {
            ProjectManager pm = new ProjectManager();
            this.Hide();
            pm.ShowDialog();
            this.Close();
        }

        private void addEvent_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = scheduleGrid.CurrentRow;

            if(row != null)
            {
                int sch_no = int.Parse(row.Cells[0].Value.ToString());
                int proj_id = int.Parse(row.Cells[1].Value.ToString());

                AddEventForm ef = new AddEventForm(sch_no, proj_id);
                ef.ShowDialog();
            }
        }

        //grid
        private void metroGrid1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        
        //search by project name
        private void searchScheduleByProjectName_TextChanged(object sender, EventArgs e)
        {

            string projName = projectName.Text.ToString();

            string sql = "select s.sch_no as Schedule_No, s.proj_id, s.visit_type_id, p.proj_name as Project_Name, vt.type as Schedule_Type, s.from_date_time as Start_Date_and_Time, s.to_date_time as End_Date_and_Time, s.to_do_list as TODO_List, s.check_list as Check_List, s.travelling_mode as Travelling_Mode, s.accommodation as Accomodation, s.meals as Meals, s.logs as Logs, c.name as Client " +
                         " from schedule s, project p, visit_type vt " +
                         " where(s.proj_id = p.proj_id) and (s.visit_type_id = vt.visit_type_id) and (p.proj_name like '%" + projName + "%') " +
                         " order by s.sch_no DESC limit 10;";

            try
            {
                MySqlDataReader reader = DBConnection.getData(sql);

                DataTable dt = new DataTable();
                dt.Load(reader);
                scheduleGrid.DataSource = dt;

                reader.Close();
            }
            catch (Exception)
            {

                throw;
            }
        }

        //search by client name
        private void searchScheduleByClientName_TextChanged(object sender, EventArgs e)
        {

            string cName = clientName.Text.ToString();

            string sql = "select s.sch_no as Schedule_No, s.proj_id, s.visit_type_id, p.proj_name as Project_Name, vt.type as Schedule_Type, s.from_date_time as Start_Date_and_Time, s.to_date_time as End_Date_and_Time, s.to_do_list as TODO_List, s.check_list as Check_List, s.travelling_mode as Travelling_Mode, s.accommodation as Accomodation, s.meals as Meals, s.logs as Logs, c.name as Client " +
                         " from schedule s, project p, visit_type vt, client c " +
                         " where (s.proj_id = p.proj_id) and (s.visit_type_id = vt.visit_type_id) and (p.client_id = c.client_id) and (c.name like '%" + cName + "%') " +
                         " order s.sch_no DESC limit 10;";

            try
            {
                MySqlDataReader reader = DBConnection.getData(sql);

                DataTable dt = new DataTable();
                dt.Load(reader);
                scheduleGrid.DataSource = dt;

                reader.Close();
            }
            catch (Exception)
            {

                throw;
            }
        }

        //getting all the relavent data for selected row
        private Schedule getScheduleRow(int schNo, int proj_id)
        {
            Schedule sch = new Schedule();
            ArrayList serviceEng = new ArrayList();
            ArrayList resoArray = new ArrayList();

            MySqlDataReader reader1 = DBConnection.getData("select * from schedule where sch_no =" + schNo + " and proj_id = " + proj_id + ";");

            if (reader1.Read()) {

                sch.ScheduleId = int.Parse(reader1.GetString("sch_no"));
                sch.ProjectOfSchedule = new Role.Project(proj_id);
                sch.Type = new EventType(int.Parse(reader1.GetString("visit_type_id")));
                sch.From = reader1.GetDateTime("from_date_time");
                sch.To = reader1.GetDateTime("to_date_time");
                sch.TodoList = reader1.GetString("to_do_list");
                sch.Resource = reader1.GetString("resource");
                sch.Checklist = reader1.GetString("check_list");
                sch.TravelMode = reader1.GetString("travelling_mode");
                sch.AccommodationMode = reader1.GetString("accommodation");
                sch.Meals = reader1.GetString("meals");
                sch.Logs = reader1.GetString("logs");

                reader1.Close();

                MySqlDataReader reader2 = DBConnection.getData("select st.staff_id, s.first_name, s.last_name from schedule_technicians st, staff s where ( st.sch_no =" + schNo + " and st.proj_id = " + proj_id + ") and (s.staff_id = st.staff_id);");

                while (reader2.Read())
                {
                    Staff staff = new Staff();
                    staff.StaffId = int.Parse(reader2.GetString("staff_id"));
                    staff.FirstName = reader2.GetString("first_name");
                    staff.LastName = reader2.GetString("last_name");
                    
                    serviceEng.Add(staff);
                }

                sch.ServEngineer = serviceEng;

                reader2.Close();

                MySqlDataReader reader3 = DBConnection.getData("select sr.resource_id, sr.qty, r.name from schedule_resources sr, resource r where ( sr.sch_no =" + schNo + " and sr.proj_id = " + proj_id + ") and (sr.resource_id = r.resource_id);");

                while (reader3.Read())
                {
                    Resource reso = new Resource();
                    reso.ResourceId = int.Parse(reader3.GetString("resource_id"));
                    reso.Name = reader3.GetString("name");
                    reso.TotalQty = int.Parse(reader3.GetString("qty"));

                    resoArray.Add(reso);
                }

                sch.ResoArray = resoArray;

                reader3.Close();

            }
            else
            {
                sch = null;
            }

            return sch;
        }

        //updating schedules
        private void updateSchedule_OnClick(object sender, EventArgs e)
        {
            DataGridViewRow row = scheduleGrid.CurrentRow;

            int sch_no = int.Parse(row.Cells[0].Value.ToString());
            int proj_id = int.Parse(row.Cells[1].Value.ToString());

            Schedule s = getScheduleRow(sch_no, proj_id);

            UpdateScheduleForm usf = new UpdateScheduleForm(s);

            usf.ShowDialog();

            scheduleGrid.DataSource = getSchedules();
        }

        //Data for grid
        private DataTable getSchedules()
        {
            DataTable dt = new DataTable();

            MySqlDataReader reader = DBConnection.getData("select s.sch_no as Schedule_No, s.proj_id, s.visit_type_id, p.proj_name as Project_Name, vt.type as Schedule_Type, s.from_date_time as Start_Date_and_Time, s.to_date_time as End_Date_and_Time, s.to_do_list as TODO_List, s.check_list as Check_List, s.travelling_mode as Travelling_Mode, s.accommodation as Accomodation, s.meals as Meals, s.logs as Logs, c.name as Client " +
                                                            "from schedule s, project p, visit_type vt, client c " +
                                                            "where (s.proj_id = p.proj_id) and (s.visit_type_id = vt.visit_type_id) and (p.client_id = c.client_id) " +
                                                            " order by s.sch_no DESC limit 10;");

            dt.Load(reader);

            reader.Close();

            totalScheduleTile();
            incompleteScheduleTile();

            return dt;
        }


        //for autocomplete project names
        private AutoCompleteStringCollection projectNameAutoComplete()
        {
            DataTable dt = new DataTable();

            MySqlDataReader reader = DBConnection.getData("select proj_name from project");
            dt.Load(reader);

            AutoCompleteStringCollection colString = new AutoCompleteStringCollection();

            foreach (DataRow item in dt.Rows)
            {
                colString.Add(Convert.ToString(item[0]));
            }

            reader.Close();

            return colString;
        }

        //for autocomplete client names
        private AutoCompleteStringCollection clientNameAutoComplete()
        {
            DataTable dt = new DataTable();

            MySqlDataReader reader = DBConnection.getData("select name from client");
            dt.Load(reader);

            AutoCompleteStringCollection colString = new AutoCompleteStringCollection();

            foreach (DataRow item in dt.Rows)
            {
                colString.Add(Convert.ToString(item[0]));
            }

            reader.Close();

            return colString;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            scheduleGrid.DataSource = getSchedules();
        }

        //data for tiles
        public void totalScheduleTile()
        {
            MySqlDataReader reader = DBConnection.getData("select count(*) as count from schedule;");

            if (reader.Read())
            {
                totalScheduels.Text = reader.GetInt16("count").ToString();
            }
            else
            {
                totalScheduels.Text = 0.ToString();
            }

            reader.Close();
        }

        public void incompleteScheduleTile()
        {
            int count = 0;

            MySqlDataReader reader = DBConnection.getData("select count(*) as count from schedule s, event e where s.sch_no = e.sch_no and s.proj_id = e.proj_id and e.to_date_time > NOW()");

            if (reader.Read())
            {
                count = reader.GetInt16("count");   
            }
            
            reader.Close();

            incompleteSchedules.Text = count.ToString();
        }

        private void reports_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = scheduleGrid.CurrentRow;

            int sch_no = int.Parse(row.Cells[0].Value.ToString());
            int proj_id = int.Parse(row.Cells[1].Value.ToString());

            ScheduleReportsForm srf = new ScheduleReportsForm(proj_id, sch_no);
            srf.ShowDialog();
        }

        private void btnIou_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = scheduleGrid.CurrentRow;

            CashIssuingForm cash = new CashIssuingForm(int.Parse(row.Cells[1].Value.ToString()),
                int.Parse(row.Cells[0].Value.ToString()),
                row.Cells[3].Value.ToString(),
                row.Cells[13].Value.ToString(),
                Convert.ToDateTime(row.Cells[5].Value.ToString()),
                Convert.ToDateTime(row.Cells[6].Value.ToString()),
                row.Cells[9].Value.ToString(),
                row.Cells[10].Value.ToString(),
                row.Cells[4].Value.ToString());

            cash.ShowDialog();
        }
    }
}
