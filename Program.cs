using System.IO; 
using Gdk;
using Gtk;
using Key = Gdk.Key;
using static Gtk.Orientation;


class NumericEntry : Entry {
    protected override void OnTextInserted(string new_text, ref int position) {
        if (int.TryParse(new_text, out int dummy))
            base.OnTextInserted(new_text, ref position);
    }
}

class Transaction{
    public DateTime date;
    public int amount;
    public string type;
    public string currency;
    public string description;

    public Transaction(DateTime date, int amount, string currency, string type, string description){
        this.date = date; this.amount = amount; this.currency = currency; this.type = type; this.description = description;
    }
}

class FindDialog : Dialog{
    static string[] find_types = {"Day", "Month", "Year"};
    Calendar cal = new Calendar();
    public ComboBoxText find_box = new ComboBoxText();
    public DateTime date;
    public FindDialog(Gtk.Window parent)
            : base("Find", parent, DialogFlags.Modal,
                   "OK", ResponseType.Ok, "Cancel", ResponseType.Cancel) {
            foreach(string s in find_types){
                find_box.AppendText(s);
            }

            date = cal.GetDate();
            find_box.Active = 0;

            Box cal_box = new Box(Horizontal,0);
            cal_box.PackStart(cal, true, true, 0);
            cal.DaySelected += get_date;

            Box combo_box = new Box(Horizontal, 5);
            Label sort_label = new Label("Find By");
            combo_box.Add(sort_label);
            combo_box.PackStart(find_box, true, true, 0);
            
            Box vbox = new Box(Vertical, 5);
            vbox.Add(cal_box);
            vbox.Add(combo_box);

            vbox.Margin = 5;
            ContentArea.Margin = 5;

            ContentArea.Add(vbox);
            ShowAll();
    
    }

    void get_date(object? sender, EventArgs args){
        date = cal.GetDate();
         
    }
}
class TransactionDialog : Dialog{
    static string[] types = {"Expense", "Income"}; 
    Calendar cal = new Calendar();
    public Button date = new Button("Date");
    public DateTime date1;
    public NumericEntry amount = new NumericEntry();
    public Entry description = new Entry();
    public ComboBoxText type = new ComboBoxText();
    
    
    

    public TransactionDialog(Gtk.Window parent, Transaction transaction, bool mode)
            : base("Edit", parent, DialogFlags.Modal,
                   "OK", ResponseType.Ok, "Cancel", ResponseType.Cancel) {
            foreach(string s in types){
                type.AppendText(s);
            } 
            cal.Date = transaction.date;
            date1 = cal.GetDate();
            amount.InsertText(transaction.amount.ToString());
            description.Text = transaction.description;
            if(transaction.type == "") type.Active = 0;
            else type.Active = Array.IndexOf(types,transaction.type);



            Box hbox = new Box(Horizontal,0);
            hbox.PackStart(cal, true, true, 0);
            cal.DaySelected += get_date;

            Grid grid = new Grid();
            Label amount_label = new Label("Amount");
            amount_label.Halign = Align.End;
            grid.Attach(amount_label, 0, 1, 1, 1);
            grid.Attach(amount, 1, 1, 1, 1);

            Label description_label = new Label("Description");
            description_label.Halign = Align.End;
            grid.Attach(description_label, 0, 2, 1, 1);
            grid.Attach(description, 1, 2, 1, 1);

            if(mode){     
                Label type_label = new Label("Type");
                grid.Attach(type_label, 0, 3, 1, 1);
                grid.Attach(type, 1, 3, 1, 1);
              
            }
            

            grid.ColumnSpacing = 2;
            grid.RowSpacing = 5;
            grid.Margin = 5;
            ContentArea.Add(hbox);
            ContentArea.Add(grid);
        
            ShowAll();
    }

    void get_date(object? sender, EventArgs args){
        date1 = cal.GetDate();
    }

}

enum Account { Balance, Savings }

class MainWindow : Gtk.Window {
    private Account account = Account.Balance;

    static string[] fields =  { "Date" , "Amount" ,"Currency", "Type", "Description"};
    List<Transaction> transactions = new List<Transaction>();
    List<Transaction> find_Transactions = new List<Transaction>();
    List<Transaction> savings = new List<Transaction>();
    ListStore store = new ListStore(typeof(string), typeof(int),typeof(string), typeof(string), typeof(string), typeof(long));
    
    ToggleButton find_button = new ToggleButton("Find");
    TreeView tree_view;
    TreeViewColumn a = new TreeViewColumn(fields[0], new CellRendererText(), "text", 0);
    Button add_button = new Button("Add");
    Button delete_button = new Button("Delete");
    Button edit_button = new Button("Edit");
    Label label1 = new Label("Balance: 0 USD");
    Label label2 = new Label("Savings: 0 USD");
    public static RadioButton balance_button = new RadioButton("Balance");
    public static RadioButton savings_button = new RadioButton(balance_button, "Savings");    

    private bool find_mode = false; 
    private DateTime find_date;
    private string find_type = "";

    public MainWindow() : base("Rafael's Magic Finance Program"){
        read_Transactions("Balance.csv", transactions);
        read_Transactions("Savings.csv", savings);
        fill(transactions);
        update_balance();
        SetDefaultSize(800, 600);

        edit_button.Sensitive = false;
        delete_button.Sensitive = false;
        
        //Change Buttons
        Box buttonBox1 = new Box(Horizontal, 20);
        buttonBox1.Add(label1);
        buttonBox1.Add(label2);
        buttonBox1.Add(balance_button);
        buttonBox1.Add(savings_button);
        buttonBox1.Add(find_button);
        balance_button.Clicked += on_balance;
        savings_button.Clicked += on_savings;
        find_button.Clicked += on_find;

        
    
        //TreeView, Horizontal box
        Box hbox = new Box(Horizontal, 0);
        tree_view = new TreeView(store);
        
        
        a.SortColumnId = 5;    
        tree_view.AppendColumn(a);
        a.SortIndicator = true;
        a.SortOrder = SortType.Ascending;
        for (int i = 1; i < 5; ++i) {
            TreeViewColumn c = new TreeViewColumn(fields[i], new CellRendererText(), "text", i);
            tree_view.AppendColumn(c);
        }
        ScrolledWindow scrolled = new ScrolledWindow();
        scrolled.Add(tree_view);
        a.Clicked += state_change;

        //ADD, DELETE, EDIT buttons
        Box buttonBox2 = new Box(Horizontal, 10);
        buttonBox2.Add(add_button);
        buttonBox2.Add(delete_button);
        buttonBox2.Add(edit_button);
        delete_button.Clicked += on_delete;
        edit_button.Clicked += on_edit;
        add_button.Clicked += on_add;
        tree_view.RowActivated += on_edit;

        tree_view.Selection.Changed += button_view;

        //VerticalBox 
        Box vbox = new Box(Vertical, 0);
        vbox.Add(buttonBox1);
        vbox.PackStart(scrolled, true, true, 10);
        vbox.Add(buttonBox2);
        vbox.Margin = 10;
        Add(vbox);
    }

    void sort_transactions(){  
        if(a.SortOrder == SortType.Descending){
            transactions.Sort((x,y) => y.date.CompareTo(x.date));  
            savings.Sort((x,y) => y.date.CompareTo(x.date));          
        }   
        else{
            transactions.Sort((x,y) => x.date.CompareTo(y.date));
            savings.Sort((x,y) => x.date.CompareTo(y.date));        
        }
        write_csv(transactions, "Balance.csv");
        write_csv(savings, "Savings.csv");
        if(find_mode && account == Account.Balance) read_find(transactions);
        else if(find_mode && !(account == Account.Balance)) read_find(savings);
        
    }
    void state_change(object? sender, EventArgs e){
        sort_transactions();
    }
    void update_balance(){
        int balance_amount = 0;
        int savings_amount = 0;

        foreach(Transaction a in transactions){
            if(a.type == "Income") balance_amount += a.amount;
            else balance_amount -= a.amount;
        }
        foreach(Transaction s in savings){
            savings_amount += s.amount;
        }

        label1.Text = ("Balance: " + balance_amount + " USD");
        label2.Text = ("Savings: " + savings_amount + " USD");
    }

    void button_view(object? sender, EventArgs args){
        TreePath[] rows = tree_view.Selection.GetSelectedRows();
        delete_button.Sensitive = edit_button.Sensitive = (rows.Length > 0);
    }
    void write_csv(List<Transaction> acc, string path){
        string line = "";
        using (StreamWriter sw = new StreamWriter(path))
                    sw.Write(line);
        using (StreamWriter sw = new StreamWriter(path, append: true))
            foreach(Transaction a in acc){
                line =(a.date + "," + a.amount + "," + a.currency + "," + a.type + "," + a.description);
                sw.WriteLine(line);
            }
    }
    void read_Transactions(string path, List<Transaction> acc) {
        using (StreamReader sr = new StreamReader(path))
            while (sr.ReadLine() is string line) {
                string[] fields = line.Split(',').Select(s => s.Trim()).ToArray();
                acc.Add(new Transaction(
                    DateTime.Parse(fields[0]), 
                    Int32.Parse(fields[1]), 
                    fields[2], fields[3], fields[4]));
            }
    }

    void read_find(List<Transaction> acc){
        find_Transactions.Clear();
        if(find_type == "Day"){
            foreach (Transaction a in acc) {
                if(find_date == a.date){
                    find_Transactions.Add(a);
                }
            }
        }
        else if(find_type == "Month"){
            string[] copy_date = find_date.ToString().Split("/");
            foreach (Transaction a in acc) {
                string[] copy_date1 = a.date.ToString().Split("/");
                if(copy_date[0] == copy_date1[0] && copy_date[2] == copy_date1[2]){
                    find_Transactions.Add(a);
                }
            }
        }
        else{
            string[] copy_date = find_date.ToString().Split("/");
            foreach (Transaction a in acc) {
                if(copy_date[2] == a.date.ToString().Split("/")[2]){
                    find_Transactions.Add(a);
                }
            }
        }
    }
    void fill(List<Transaction> acc) {
        store.Clear();
        foreach (Transaction a in acc) {
            TreeIter i = store.Append();
            store.SetValues(i, a.date.ToString("MM/dd/yyyy"), a.amount, a.currency, a.type, a.description, a.date.Ticks);
        }
    }


    void on_find(object? sender, EventArgs args){
        if(find_button.Active){
            using(FindDialog d = new FindDialog(this)){
                if(d.Run() == (int) ResponseType.Ok){
                    find_mode = true;
                    find_date = d.date;
                    find_type = d.find_box.ActiveText;
                    
                    if(account == Account.Balance) read_find(transactions);
                    else read_find(savings);
                    fill(find_Transactions);

                }
                else find_button.Active = false;
            }  
        }
        else{
            
            if(balance_button.Active) fill(transactions);
            else fill(savings);
        }
    }
    void on_balance(object? sender, EventArgs args){
        fill(transactions);
        account = Account.Balance;
        if(find_mode){
            find_mode = false;
            find_button.Active = false;
        }
     }

    void on_savings(object? sender, EventArgs args){
        fill(savings);
        account = Account.Savings;
        if(find_mode){
            find_mode = false;
            find_button.Active = false;
        }
    }

    void add_help(List<Transaction> acc, string path){
        string line = "";
        DateTime a = DateTime.Today; 
        acc.Add(new Transaction(a, 0,"USD", "", ""));
        int index = acc.Count() - 1;
        using (TransactionDialog d = new TransactionDialog(this, acc[index], account == Account.Balance)){
            if(d.Run() == (int) ResponseType.Ok){
                if(d.amount.Text == ""){
                    acc.RemoveAt(index);
                    return;
                }
                acc[index].date = d.date1;
                acc[index].amount = Int32.Parse(d.amount.Text);
                acc[index].description = d.description.Text;
                if(account == Account.Balance){
                    acc[index].type = d.type.ActiveText;
                }
                else{
                    acc[index].type = "Savings";
                }
                line =(acc[index].date + "," + acc[index].amount + "," + acc[index].currency + "," + acc[index].type + "," +acc[index].description);
                sort_transactions();
                if(find_mode) {
                    
                    read_find(acc);
                    fill(find_Transactions);
                }
                else fill(acc);
            }
            else{
                acc.RemoveAt(index);
            }
        }
    }
    void on_add(object? sender, EventArgs args){
        if(account == Account.Balance) add_help(transactions, "Balance.csv");
        else add_help(savings, "Savings.csv");
        update_balance();
    }

    void on_delete_help(List<Transaction> account, string path){
        TreePath[] rows = tree_view.Selection.GetSelectedRows();
            if (rows.Length > 0) {
                int row_index = rows[0].Indices[0];
                if(!find_mode){
                    account.RemoveAt(row_index);
                    fill(account);
                }
                else {
                    account.Remove(find_Transactions[row_index]);
                    find_Transactions.RemoveAt(row_index);
                    fill(find_Transactions);
                }  
                write_csv(account, path);    
                update_balance();
            }
    }
    void on_delete(object? sender, EventArgs args){
        if(account == Account.Balance) on_delete_help(transactions, "Balance.csv");
        else on_delete_help(savings, "Savings.csv");
    }
    protected override bool OnKeyPressEvent(EventKey e) {
        if (e.Key == Key.Delete) {
            if(account == Account.Balance) on_delete_help(transactions, "Balance.csv");
            else on_delete_help(savings, "Savings.csv");
        }
        return true;
    }
    void edit_help(List<Transaction> acc, string path, int index){
        using (TransactionDialog d = new TransactionDialog(this, acc[index], account == Account.Balance)){
            if(d.Run() == (int) ResponseType.Ok){
                if(d.amount.Text == ""){
                    return;
                }
                acc[index].date = d.date1;
                acc[index].amount =  Int32.Parse(d.amount.Text);
                acc[index].description = d.description.Text;
                if(account == Account.Balance){
                    acc[index].type = d.type.ActiveText;
                }
                else acc[index].type = "Savings";
                sort_transactions();
                if(find_mode) {
                    
                    read_find(acc);
                    fill(find_Transactions);
                }
                else fill(acc);
            
            }
        }
    }
    void on_edit(object? sender, EventArgs args) {
        int index = 0;
        TreePath[] rows = tree_view.Selection.GetSelectedRows();   
        if(rows.Length > 0) index = rows[0].Indices[0]; 
        else return;
        if(account == Account.Balance) {
            if(find_mode) edit_help(transactions, "Balance.csv", transactions.IndexOf(find_Transactions[index]));
            else edit_help(transactions, "Balance.csv", index);
        }
        else {
            if(find_mode) edit_help(savings, "Balance.csv", savings.IndexOf(find_Transactions[index]));
            else edit_help(savings, "Savings.csv", index);
        }
        update_balance();
    }

    protected override bool OnDeleteEvent(Event e) {
        Application.Quit();
        return true;
    }
}
class Top {
    static void Main() {
        string filename = "Balance.csv";
        string filename2 = "Savings.csv";
        if(!File.Exists(Directory.GetCurrentDirectory() + "/" + filename)) File.Create(filename).Close();
        if(!File.Exists(Directory.GetCurrentDirectory() + "/" + filename2)) File.Create(filename2).Close();
        Application.Init();
        MainWindow w = new MainWindow();
        w.ShowAll();
        Application.Run();
    }
}
