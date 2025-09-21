# Sage Evolution SDK (C# Transactions) – Technical User Guide

## Scope
This document assembles the complete C#-based transaction guide from the Sage Evolution SDK user guide. The content is reproduced from the official documentation for the C# SDK transactions without summary so that developers can reference the exact examples and instructions. The guide covers Accounts Payable, Accounts Receivable, General Ledger, Inventory, Order Entry, Contact Management Incidents, Job Costing and Additional Functionality. Each section below reproduces the full description and code examples from the corresponding user-guide pages.

## Accounts Payable

### 1. Supplier Account
The `Supplier` class lets you create supplier accounts for supplier-related processes. A supplier is created by setting properties (code, description, etc.) and then calling `Save()`. The same supplier can later be edited and saved again with new information such as telephone numbers or addresses. The documentation provides the following examples:

```csharp
//Assign variable S to Supplier class
Supplier S = new Supplier();
//Specify Supplier properties
S.Code = "SupplierSDK1";
S.Description = "supplierSDK1";
//Use the save method to Save the Supplier
S.Save();
```

Editing an existing supplier involves loading it by code, updating properties and calling `Save()`:

```csharp
Supplier S = new Supplier("SupplierSDK1");
//Set new properties
S.Telephone = "113456";
S.EmailAddress = "Supplier@SDK";
//Set Postal or physical address
S.PostalAddress = new Address("Postal Address 1", "Post 2", "Post 3",
"Post 4", "Post 5", "PC");
S.PhysicalAddress = new Address()
{
    Line1 = "Physical1",
    Line2 = "Physical2",
    Line3 = "Physical3",
    Line4 = "Physical4",
    Line5 = "Physical5",
    PostalCode = "2000",
};
//Use the save method to Save the Supplier
S.Save();
```

Source: CSupplierAccountTransaction page.

### 2. Supplier Transactions
A `SupplierTransaction` is the SDK equivalent of a standard transaction in `Evolution → Accounts Payable → Transactions → Standard`. These transactions affect both the General Ledger and the Creditors Ledger and are typically used to post payments to suppliers (invoices posted here do not affect inventory or store as a source document). Only the `Account`, `Reference` and `TransactionCode` fields are required. The transaction code defines whether the transaction is a debit or credit and determines which GL accounts to post to. The `Post()` method processes the transaction immediately.

Example of a supplier transaction using an `IN` transaction type:

```csharp
// Declare Supplier Transaction Class
SupplierTransaction SuppTran = new SupplierTransaction();
//Instance of Supplier class
SuppTran.Supplier = new Supplier("Supplier1");
SuppTran.TransactionCode = new TransactionCode(Module.AP, "IN");
SuppTran.TaxRate = new TaxRate("1");
SuppTran.Amount = 100;
SuppTran.Reference = "AP_Ref1";
SuppTran.Description = "StringDescription";
//Post Method to Commit Transaction
SuppTran.Post();
```

The documentation also shows how to post multiple transactions as a batch using the same audit number. It demonstrates creating several `SupplierTransaction` objects, handling events for debit and credit posting, adding transactions to a GL batch and posting the batch inside a transaction scope:

```csharp
{
    ConsolidateSuppliers();
}

private static GLBatch _batch = new GLBatch();

public static void ConsolidateSuppliers()
{
    try
    {
        DatabaseContext.BeginTran();
        _batch.Clear();

        //Transaction 1
        SupplierTransaction supptran = new SupplierTransaction();
        supptran.GLDebitPosting += new TransactionBase.GLPostingEventHandler(supptran_GLDebitPosting);
        supptran.GLCreditPosting += new TransactionBase.GLPostingEventHandler(supptran_GLCreditPosting);
        supptran.Account = new Supplier("Supplier1");
        supptran.Amount = 50;
        supptran.TaxRate = new TaxRate(7);
        supptran.Reference = supptran.Description = "inv1";
        supptran.TransactionCode = new TransactionCode(Module.AP, "IN");
        supptran.Post();

        //Transaction 2
        supptran = new SupplierTransaction();
        supptran.GLDebitPosting += new TransactionBase.GLPostingEventHandler(supptran_GLDebitPosting);
        supptran.GLCreditPosting += new TransactionBase.GLPostingEventHandler(supptran_GLCreditPosting);
        supptran.Account = new Supplier("Supplier1");
        supptran.Amount = 60;
        supptran.TaxRate = new TaxRate(7);
        supptran.Reference = supptran.Description = "inv2";
        supptran.TransactionCode = new TransactionCode(Module.AP, "IN");
        supptran.Post();

        //Transaction 3
        supptran = new SupplierTransaction();
        supptran.GLDebitPosting += new TransactionBase.GLPostingEventHandler(supptran_GLDebitPosting);
        supptran.GLCreditPosting += new TransactionBase.GLPostingEventHandler(supptran_GLCreditPosting);
        supptran.Account = new Supplier("Supplier1");
        supptran.Amount = 70;
        supptran.TaxRate = new TaxRate(7);
        supptran.Reference = supptran.Description = "inv3";
        supptran.TransactionCode = new TransactionCode(Module.AP, "IN");
        supptran.Post();

        _batch.Post();
        DatabaseContext.CommitTran();
    }
    catch (Exception ex)
    {
        DatabaseContext.RollbackTran();
        MessageBox.Show(ex.Message);
    }
}

static void supptran_GLCreditPosting(TransactionBase sender, TransactionBase.GLPostingEventArgs e)
{
    _batch.Add((GLTransaction)e.GLTransaction.Clone());
    e.Posted = true;
}

static void supptran_GLDebitPosting(TransactionBase sender, TransactionBase.GLPostingEventArgs e)
{
    _batch.Add((GLTransaction)e.GLTransaction.Clone(), true, true);
    e.Posted = true;
}
```

Source: CSupplierTransaction page.

### 3. Supplier Allocations
Supplier transactions (e.g., invoices) can be allocated to contra transactions (e.g., payments). All supplier transactions appear in the `PostAP` table; the `AccountLink` field identifies the supplier and the `Outstanding` field shows the unallocated amount. A fully allocated transaction has an `Outstanding` value of 0. The `calloc` field holds allocation data (e.g., `I=6;A=50;D=20150622`, where `I` is the auto index, `A` is the amount allocated and `D` is the date).

To allocate two transactions you need the `AutoIdx` values for both. Example:

```csharp
SupplierTransaction invoice = new SupplierTransaction(5);// AutoIdx of debit transaction in PostAP table
SupplierTransaction payment = new SupplierTransaction(8);// AutoIdx of credit transaction in PostAP table
invoice.Allocations.Add(payment);
invoice.Allocations.Save();
```

Allocations can also be performed at the time of posting:

```csharp
//An invoice is posted.
SupplierTransaction invoice = new SupplierTransaction();
invoice.Account = new Supplier("Supplier1");
invoice.TransactionCode = new TransactionCode(Module.AP, "IN");
invoice.Amount = 500.50;
invoice.TaxRate = new TaxRate(7);
invoice.Reference = "SINV12348902";
invoice.Description = "Supplier Invoice";
invoice.Post();

//A payment is posted.
SupplierTransaction payment = new SupplierTransaction();
payment.Account = new Supplier("Supplier1");
payment.TransactionCode = new TransactionCode(Module.AP, "PM");
payment.Amount = 1000.00;
payment.Reference = "EFT";
payment.Description = "Payment";
payment.Post();

//Allocate the in-memory payment to the invoice
invoice.Allocations.Add(payment);
//Call Save to commit the allocation
invoice.Allocations.Save();
```

The following example allocates a supplier payment to an existing order invoice by obtaining the transaction ID for the order and then allocating:

```csharp
{
    //Post a supplier payment
    SupplierTransaction payment = new SupplierTransaction();
    payment.Account = new Supplier("Supplier1");
    payment.TransactionCode = new TransactionCode(Module.AP, "PM");
    payment.OrderNo = "PO0018";
    payment.Amount = 20;
    payment.Reference = "PM123";
    payment.Description = "Payment Made";
    payment.Post();

    //Use method allocateOrder to find order number and allocate to payment
    allocateOrder(payment);
}

private void allocateOrder(DrCrTransaction transaction)
{
    string criteria = string.Format("Order_No = '{0}'", transaction.OrderNo);
    if (transaction.Outstanding == 0)
        return;
    if (transaction.Outstanding > 0)
        criteria += " and Outstanding < 0";
    else
        criteria += " and Outstanding > 0";
    DataTable matches = SupplierTransaction.List(transaction.Account, criteria);
    {
        if (matches.Rows.Count > 0)
            foreach (DataRow match in matches.Rows)
            {
                //terminate if satisfied
                if (transaction.Outstanding == 0)
                    break;
                SupplierTransaction relatedTran = new SupplierTransaction((Int64)match["Autoidx"]);
                transaction.Allocations.Add(relatedTran);
            }
        transaction.Allocations.Save();
    }
}
```

Source: CSupplierAllocations page.

### 4. Supplier Batches
Supplier batches are created under `Suppliers → Transactions → Supplier Batches`. A batch can include General Ledger (GL) accounts and customer accounts, and it can be processed. The example below demonstrates creating a supplier batch code, populating it with multiple lines (customer and supplier lines, GL lines with contra splits) and processing the batch.

```csharp
string SBatchNum = "APB1";
SupplierBatch.Get(1);
if (SupplierBatch.Find(SBatchNum) == -1)
{
    // Batch code does not exist, so create it
    SupplierBatch createsbatch = new SupplierBatch();
    createsbatch.BatchNo = SBatchNum;
    createsbatch.Description = "SB Batch";
    createsbatch.CreatedAgent = new Agent("Admin");
    createsbatch.AllowGLContraSplit = true;
    createsbatch.AllowDuplicateReferences = true;
    createsbatch.EnterTaxOnGlContraSplit = true;
    createsbatch.Save();
    MessageBox.Show(createsbatch.BatchNo);
}

//Populate and process the supplier batch
SupplierBatch SB = new SupplierBatch(SBatchNum);
BatchDetail BDet = new BatchDetail();
BDet.Customer = new Customer("Cash");
BDet.Date = DateTime.Now;
BDet.Description = "LineDesc";
BDet.Reference = "ref1";
BDet.PostDated = false;
BDet.TransactionCode = new TransactionCode(Module.AR, "IN");
BDet.TaxType = new TaxRate(1);
BDet.AmountExclusive = 728;
BDet.GLContraAccount = new GLAccount("accounting fees");
SB.Detail.Add(BDet);

BDet = new BatchDetail();
SB.Detail.Add(BDet);
BDet.Supplier = new Supplier("Supplier1");
BDet.Date = DateTime.Now;
BDet.Description = "LineDesc";
BDet.Reference = "ref1";
BDet.PostDated = false;
BDet.TransactionCode = new TransactionCode(Module.AP, "IN");
BDet.TaxType = new TaxRate(1);
BDet.ContraSplit.Add("Security", "Ledger Line 1", 100, "1");
BDet.ContraSplit.Add("Sales", "Ledger Line 2", 328, "1");
BDet.AmountExclusive = 428;

BDet = new BatchDetail();
BDet.GLAccount = new GLAccount("Advertising");
BDet.Date = DateTime.Now;
BDet.Description = "LineDesc";
BDet.Reference = "ref1";
BDet.PostDated = false;
BDet.IsDebit = true;
BDet.TransactionCode = new TransactionCode(Module.GL, "JNL");
BDet.TaxType = new TaxRate(1);
BDet.ContraSplit.Add("Security", "Ledger Line 1", 100, "1");
BDet.ContraSplit.Add("Sales", "Ledger Line 2", 428, "1");
BDet.ContraSplit.Add("Accounting fees", "Ledger Line 3", 500, "3");
BDet.AmountExclusive = 1028;
SB.Detail.Add(BDet);
SB.Process();
```

Source: CSupplierBatches page.

## Accounts Receivable

### 1. Customer Account
The `Customer` class lets you create customer accounts for customer-related processes. A new customer is created by setting properties such as code and description and calling `Save()`. The example below illustrates the creation of a customer and subsequently editing the customer to add addresses and delivery addresses:

```csharp
//Assign variable C to Customer class
Customer C = new Customer();
//Specify Customer properties
C.Code = "CustomerSDK1";
C.Description = "supplierSDK1";
//Use the save method to Save the Customer
C.Save();
```

To edit or enhance a customer with telephone numbers, email addresses, postal/physical addresses and delivery addresses, the customer must already exist. The code checks for existence of the customer and delivery code, then updates the details and saves:

```csharp
string DC = "Del2";
string Cust = "CustomerSDK1";

//check if Customer exists – note to capture a delivery address the Customer must exist first
if (Customer.FindByCode(Cust) == -1)
{
    Customer NewCust = new Customer();
    //NewCust.Description = Cust;
    NewCust.Description = Cust;
    NewCust.Save();
}

Customer C = new Customer(Cust);
//Set new properties
C.Telephone = "113456";
C.EmailAddress = "Customer@SDK";
//Set Postal or physical address
C.PostalAddress = new Address("Postal Address 1", "Post 2", "Post 3",
"Post 4", "Post 5", "PC");
C.PhysicalAddress = new Address()
{
    Line1 = "Physical1",
    Line2 = "Physical2",
    Line3 = "Physical3",
    Line4 = "Physical4",
    Line5 = "Physical5",
    PostalCode = "2000",
};

//Check for Delivery Address code – if false then save
if (DeliveryAddressCode.FindByCode(DC) == -1)
{
    DeliveryAddressCode delAdd = new DeliveryAddressCode();
    delAdd.Code = DC;
    delAdd.Description = ("Delivery address");
    delAdd.Save();
}

//Specify the DeliveryAddress address for the code
Address address = new Address("102 Delivery Address", "Delivery Address 2",
"2620");
C.DeliveryAddresses.Add(DC, address);
//Use the save method to Save the Customer
C.Save();
```

Source: C Customer Account Transaction page.

### 2. Customer Transactions
A `CustomerTransaction` is analogous to a standard transaction in `Evolution → Accounts Receivable → Transactions → Standard`. These transactions affect the General Ledger and Debtors Ledger and are typically used to post payments (receipts) for customers; invoices posted here do not affect inventory or serve as source documents. Required values are `Account`, `Reference` and `TransactionCode`. The transaction code defines whether the transaction is a debit or credit and determines GL posting. The `Post()` method processes the transaction and adjusts the account balance. Default transaction codes exist (e.g., `IN` and `CN`) but you can configure additional codes.

Example of a customer transaction using the `IN` transaction type:

```csharp
// Declare Customer Transaction Class
CustomerTransaction CustTran = new CustomerTransaction();
//Instance of Customer class
CustTran.Customer = new Customer("Customer1");
CustTran.TransactionCode = new TransactionCode(Module.AR, "IN");
CustTran.TaxRate = new TaxRate("1");
CustTran.Amount = 100;
CustTran.Reference = "AP_Ref1";
CustTran.Description = "StringDescription";
//Post Method to Commit Transaction
CustTran.Post();
```

Posting customer transactions as a batch uses a GL batch and transaction scope similar to supplier transactions. The example below posts three transactions and adds their GL transactions to a batch via event handlers, then posts the batch and commits the transaction:

```csharp
{
    ConsolidateCustomers();
}

private static GLBatch _batch = new GLBatch();

public static void ConsolidateCustomers()
{
    try
    {
        DatabaseContext.BeginTran();
        _batch.Clear();

        //Transaction 1
        CustomerTransaction custtran = new CustomerTransaction();
        custtran.GLDebitPosting += new TransactionBase.GLPostingEventHandler(custtran_GLDebitPosting);
        custtran.GLCreditPosting += new TransactionBase.GLPostingEventHandler(custtran_GLCreditPosting);
        custtran.Account = new Customer("Customer1");
        custtran.Amount = 50;
        custtran.TaxRate = new TaxRate(7);
        custtran.Reference = custtran.Description = "inv1";
        custtran.TransactionCode = new TransactionCode(Module.AR, "IN");
        custtran.Post();

        //Transaction 2
        custtran = new CustomerTransaction();
        custtran.GLDebitPosting += new TransactionBase.GLPostingEventHandler(custtran_GLDebitPosting);
        custtran.GLCreditPosting += new TransactionBase.GLPostingEventHandler(custtran_GLCreditPosting);
        custtran.Account = new Customer("Customer1");
        custtran.Amount = 60;
        custtran.TaxRate = new TaxRate(7);
        custtran.Reference = custtran.Description = "inv2";
        custtran.TransactionCode = new TransactionCode(Module.AR, "IN");
        custtran.Post();

        //Transaction 3
        custtran = new CustomerTransaction();
        custtran.GLDebitPosting += new TransactionBase.GLPostingEventHandler(custtran_GLDebitPosting);
        custtran.GLCreditPosting += new TransactionBase.GLPostingEventHandler(custtran_GLCreditPosting);
        custtran.Account = new Supplier("Customer1");
        custtran.Amount = 70;
        custtran.TaxRate = new TaxRate(7);
        custtran.Reference = custtran.Description = "inv3";
        custtran.TransactionCode = new TransactionCode(Module.AR, "IN");
        custtran.Post();

        _batch.Post();
        DatabaseContext.CommitTran();
    }
    catch (Exception ex)
    {
        DatabaseContext.RollbackTran();
        MessageBox.Show(ex.Message);
    }
}

static void custtran_GLCreditPosting(TransactionBase sender, TransactionBase.GLPostingEventArgs e)
{
    _batch.Add((GLTransaction)e.GLTransaction.Clone());
    e.Posted = true;
}

static void custtran_GLDebitPosting(TransactionBase sender, TransactionBase.GLPostingEventArgs e)
{
    _batch.Add((GLTransaction)e.GLTransaction.Clone(), true, true);
    e.Posted = true;
}
```

Source: C Customer Transaction page.

### 3. Customer Allocations
Customer invoices can be allocated to contra transactions (e.g., payment receipts) in the same way as supplier allocations. All customer transactions appear in the `PostAR` table. The `AccountLink` field identifies the customer and the `Outstanding` field shows the remaining amount; a fully allocated transaction has an outstanding value of zero. The `calloc` field stores allocation data.

To allocate two transactions by auto index:

```csharp
CustomerTransaction invoice = new CustomerTransaction(5);// AutoIdx of debit transaction in PostAR table
CustomerTransaction payment = new CustomerTransaction(8);// AutoIdx of credit transaction in PostAR table
invoice.Allocations.Add(payment);
invoice.Allocations.Save();
```

Allocations can also occur at posting time:

```csharp
//An invoice is posted.
CustomerTransaction invoice = new CustomerTransaction();
invoice.Account = new Customer("Customer1");
invoice.TransactionCode = new TransactionCode(Module.AR, "IN");
invoice.Amount = 500.50;
invoice.TaxRate = new TaxRate(7);
invoice.Reference = "INV12348902";
invoice.Description = "Customer Invoice";
invoice.Post();

//A payment (receipt) is posted.
CustomerTransaction payment = new CustomerTransaction();
payment.Account = new Customer("Customer1");
payment.TransactionCode = new TransactionCode(Module.AR, "PM");
payment.Amount = 1000.00;
payment.Reference = "EFT";
payment.Description = "Payment";
payment.Post();

//Allocate the payment to the invoice
invoice.Allocations.Add(payment);
//Save to commit allocation
invoice.Allocations.Save();
```

The following example allocates a customer payment to an existing sales order invoice by finding the order number and allocating to the payment:

```csharp
{
    //Post a Customer payment
    CustomerTransaction payment = new CustomerTransaction();
    payment.Account = new Customer("Customer1");
    payment.TransactionCode = new TransactionCode(Module.AR, "PM");
    payment.OrderNo = "SO0018";
    payment.Amount = 20;
    payment.Reference = "PM123";
    payment.Description = "Payment Made";
    payment.Post();

    //Use method allocateOrder to find order number and allocate to payment
    allocateOrder(payment);
}

private void allocateOrder(DrCrTransaction transaction)
{
    string criteria = string.Format("Order_No = '{0}'", transaction.OrderNo);
    if (transaction.Outstanding == 0)
        return;
    if (transaction.Outstanding > 0)
        criteria += " and Outstanding < 0";
    else
        criteria += " and Outstanding > 0";
    DataTable matches = CustomerTransaction.List(transaction.Account, criteria);
    {
        if (matches.Rows.Count > 0)
            foreach (DataRow match in matches.Rows)
            {
                //terminate if satisfied
                if (transaction.Outstanding == 0)
                    break;
                CustomerTransaction relatedTran = new CustomerTransaction((Int64)match["Autoidx"]);
                transaction.Allocations.Add(relatedTran);
            }
        transaction.Allocations.Save();
    }
}
```

Source: C Customer Allocations page.

### 4. Customer Batches
Customer batches are created under `Customers → Transactions → Customer Batches`. A batch can include GL accounts and supplier accounts and can be processed. The example below shows creating a batch code (if it does not exist), then adding multiple batch detail lines (customer line, supplier line, GL line) with contra splits and processing the batch:

```csharp
string BatchNum = "ARB1";
CustomerBatch.Get(1);
if (CustomerBatch.Find(BatchNum) == -1)
{
    // Batch code does not exist, so create it
    CustomerBatch createsbatch = new CustomerBatch();
    createsbatch.BatchNo = BatchNum;
    createsbatch.Description = "CB Batch";
    createsbatch.CreatedAgent = new Agent("Admin");
    createsbatch.AllowGLContraSplit = true;
    createsbatch.AllowDuplicateReferences = true;
    createsbatch.EnterTaxOnGlContraSplit = true;
    createsbatch.Save();
    MessageBox.Show(createsbatch.BatchNo);
}

//Populate and process the customer batch
CustomerBatch CB = new CustomerBatch(BatchNum);
BatchDetail BDet = new BatchDetail();
BDet.Customer = new Customer("Cash");
BDet.Date = DateTime.Now;
BDet.Description = "LineDesc";
BDet.Reference = "ref1";
BDet.PostDated = false;
BDet.TransactionCode = new TransactionCode(Module.AR, "IN");
BDet.TaxType = new TaxRate(1);
BDet.AmountExclusive = 728;
BDet.GLContraAccount = new GLAccount("accounting fees");
CB.Detail.Add(BDet);

BDet = new BatchDetail();
CB.Detail.Add(BDet);
BDet.Supplier = new Supplier("Supplier1");
BDet.Date = DateTime.Now;
BDet.Description = "LineDesc";
BDet.Reference = "ref1";
BDet.PostDated = false;
BDet.TransactionCode = new TransactionCode(Module.AP, "IN");
BDet.TaxType = new TaxRate(1);
BDet.ContraSplit.Add("Security", "Ledger Line 1", 100, "1");
BDet.ContraSplit.Add("Sales", "Ledger Line 2", 328, "1");
BDet.AmountExclusive = 428;

BDet = new BatchDetail();
BDet.GLAccount = new GLAccount("Advertising");
BDet.Date = DateTime.Now;
BDet.Description = "LineDesc";
BDet.Reference = "ref1";
BDet.PostDated = false;
BDet.IsDebit = true;
BDet.TransactionCode = new TransactionCode(Module.GL, "JNL");
BDet.TaxType = new TaxRate(1);
BDet.ContraSplit.Add("Security", "Ledger Line 1", 100, "1");
BDet.ContraSplit.Add("Sales", "Ledger Line 2", 428, "1");
BDet.ContraSplit.Add("Accounting fees", "Ledger Line 3", 500, "3");
BDet.AmountExclusive = 1028;
CB.Detail.Add(BDet);
CB.Process();
```

Source: C Customer Batches page.

## General Ledger

### 1. General Ledger Accounts
The `GLAccount` class allows the creation of General Ledger accounts used in ledger processes. The example below shows how to create a new GL account by specifying its code and account type and then saving it:

```csharp
//Assign variable gl to GLAccount class
GLAccount gl = new GLAccount();
//Specify Account properties like code and Account Type
gl.Code = "SDKAccount";
gl.Type = GLAccount.AccountType.CurrentAsset;
//Use the save method to Save the Account
gl.Save();
```

Source: C General Ledger Accounts page.

### 2. General Ledger Transactions
Ledger transactions can be posted through the SDK using the `GLTransaction` class. This is equivalent to posting a journal in Evolution. The example shows posting a debit and credit transaction (with tax) inside a transaction scope. Each GL transaction requires specifying the account, amount (debit or credit), date, description, reference and transaction code. The tax leg of the credit transaction is posted separately with the `ModuleID.Tax` flag.

```csharp
{
    DatabaseContext.BeginTran();
    GLTransaction GLDr = new GLTransaction();
    GLDr.Account = new GLAccount("Sales"); // specify the GL Account
    GLDr.Debit = 100 + 14;
    GLDr.Date = DateTime.Today;
    GLDr.Description = "descr";
    GLDr.Reference = "ref1";
    GLDr.TransactionCode = new TransactionCode(Module.GL, "JNL"); // Specify the GL transaction code
    GLDr.Post();

    GLTransaction GLCr = new GLTransaction();
    GLCr.Account = new GLAccount("Accruals"); // specify the GL Account
    GLCr.Credit = 100;
    GLCr.TaxRate = new TaxRate(1);
    //GLCr.Tax = 14; //Tax Amount can be specified if required
    GLCr.Date = DateTime.Today;
    GLCr.Description = "descr";
    GLCr.Reference = "ref1";
    GLCr.TransactionCode = new TransactionCode(Module.GL, "JNL"); // Specify the GL transaction code
    GLCr.Post();

    //Posting the VAT leg of the credit transaction above
    GLTransaction Tax = new GLTransaction();
    Tax.Account = new GLAccount("Vat Control"); // specify the GL Tax Account
    Tax.Credit = 14;
    //Tax.Tax = GLCr.Tax;
    Tax.Date = DateTime.Today;
    Tax.ModID = ModuleID.Tax; // Specify this transaction id to be tax
    Tax.Description = "descr";
    Tax.Reference = "ref1";
    Tax.TransactionCode = new TransactionCode(Module.GL, "JNL"); // Specify the GL transaction code
    Tax.Post();
    DatabaseContext.CommitTran();
}
```

Source: C General Ledger Transactions page.

### 3. Cashbook Batches
The SDK allows creating and populating cashbook batches but does not allow processing them via the SDK. The example shows how to create a new cashbook batch code, add multiple cashbook batch detail lines for ledger, payable and receivable modules, and save each line:

```csharp
{
    string BatchNum = "CB001";
    //check if Batch code exists in pastel
    if (CashbookBatch.FindByCode(BatchNum) == -1)
    {
        // Batch code does not exist, so create it
        CashbookBatch createbatch = new CashbookBatch();
        createbatch.Code = BatchNum;
        createbatch.Description = "CB Batch";
        createbatch.Owner = new Agent("Admin");
        createbatch.Save();
    }

    CashbookBatch batch = new CashbookBatch(BatchNum);
    CashbookBatchDetail detail = new CashbookBatchDetail();
    detail.Date = DateTime.Today;
    detail.LineModule = CashbookBatchDetail.Module.Ledger; //Specify the line module
    detail.Account = new GLAccount("Advertising");
    detail.Credit = 500;
    detail.Description = "Ledger Transaction";
    detail.Reference = "Ref001";
    detail.Tax = 50;
    detail.TaxType = new TaxRate(1); //Specify a tax type
    detail.TaxAccount = new GLAccount("Accruals"); //Specify a tax account
    batch.Detail.Add(detail);
    batch.Save(); //Save each CashbookBatchDetail separately or comment out to save all together

    detail = new CashbookBatchDetail();
    detail.Date = DateTime.Today;
    detail.LineModule = CashbookBatchDetail.Module.Payables;
    detail.Supplier = new Supplier("Supplier1");
    detail.Debit = 500;
    detail.Description = "Supplier Transaction";
    detail.Reference = "Ref001";
    batch.Detail.Add(detail);
    batch.Save();

    detail = new CashbookBatchDetail();
    detail.Date = DateTime.Today;
    detail.LineModule = CashbookBatchDetail.Module.Receivables;
    detail.Customer = new Customer("Customer1");
    detail.Debit = 500;
    detail.Description = "Customer Transaction";
    detail.Reference = "Ref001";
    batch.Detail.Add(detail);
    batch.Save();

    //If required the clone method can be used to copy the previous line
    batch = new CashbookBatch(batch.Code);
    batch.Detail.Add(detail.Clone());
    batch.Save();
}
```

Source: C Cashbook Batches page.

### 4. Journal Batches
Journal batches can be created and populated via the SDK but cannot be processed. The following example shows creating a journal batch code if it does not exist, adding a debit and credit line (with tax) to the batch, and saving each detail. The clone method can duplicate lines when needed:

```csharp
{
    string BatchNum = "JB002";
    //check if Batch code exists in pastel
    if (JournalBatch.FindByCode(BatchNum) == -1)
    {
        // Batch code does not exist, then create it
        JournalBatch createbatch = new JournalBatch();
        createbatch.Code = BatchNum;
        createbatch.Description = "JB Batch";
        createbatch.Owner = new Agent("Admin");
        createbatch.Save();
    }

    JournalBatch batch = new JournalBatch(BatchNum);
    var detail = new JournalBatchDetail();
    detail.Date = DateTime.Today;
    detail.Account = new GLAccount("Advertising");
    detail.Debit = 500;
    detail.Description = "So and so";
    detail.Reference = "Ref001";
    batch.Detail.Add(detail);
    batch.Save(); //Save each JournalBatchDetail separately or comment out to save both together

    detail = new JournalBatchDetail();
    detail.Date = DateTime.Today;
    detail.Account = new GLAccount("Advertising");
    detail.Credit = 500;
    detail.Description = "So and so";
    detail.Reference = "Ref001";
    detail.Tax = 50;
    detail.TaxRate = new TaxRate(1); //Specify a tax type
    detail.TaxAccount = new GLAccount("Accruals"); //Specify a tax account
    batch.Detail.Add(detail);
    batch.Save();

    //If required the clone method can be used to copy the previous line
    batch = new JournalBatch(batch.Code);
    batch.Detail.Add(detail.Clone());
    batch.Save();
}
```

Source: C Journal Batches page.

## Inventory

### 1. Inventory Item
To create a new inventory item, instantiate the `InventoryItem` class, set properties (code, descriptions) and specify price lists and selling prices before calling `Save()`.

```csharp
//Create an instance of the InventoryItem class
InventoryItem invItem = new InventoryItem();
invItem.Code = "TestSDK9";
invItem.Description = "TestSDK9";
invItem.Description_2 = "Description2";
invItem.Description_3 = "Description3";
invItem.IsWarehouseTracked = true; // Properties like whether the item is a warehouse item can be specified

//Prices can also be specified as follows
PriceList p1 = new PriceList("Price List 1");
PriceList p2 = new PriceList("Price List 2");
PriceList p3 = new PriceList("Price List 3");
PriceList p4 = new PriceList("New Price 4");

invItem.SellingPrices[p1].PriceExcl = 200;
invItem.SellingPrices[p2].PriceExcl = 300;
invItem.SellingPrices[p3].PriceExcl = 400;
invItem.SellingPrices[p4].PriceExcl = 500;

//Save the item
invItem.Save();
```

Source: C Inventory Item page.

### 2. Inventory Transactions (Adjustments)
The `InventoryTransaction` class is used to perform inventory adjustments – increasing, decreasing or adjusting cost. The example below shows posting an increase transaction and a cost adjustment:

```csharp
//Create an instance of the InventoryTransaction class
InventoryTransaction ItemInc = new InventoryTransaction();
ItemInc.TransactionCode = new TransactionCode(Module.Inventory, "ADJ"); // specify a inventory transaction type generally ADJ
ItemInc.InventoryItem = new InventoryItem("Item1");
ItemInc.Operation = InventoryOperation.Increase; //Select the necessary enumerator increase, decrease or cost adjustment
ItemInc.Quantity = 2;
ItemInc.Reference = "F2";
ItemInc.Reference2 = "ref2";
ItemInc.Description = "desc";
ItemInc.Post();

//Create a cost adjustment transaction
InventoryTransaction ITCost = new InventoryTransaction();
ITCost.TransactionCode = new TransactionCode(Module.Inventory, "ADJ");
ITCost.InventoryItem = new InventoryItem("Item1");
ITCost.Operation = InventoryOperation.CostAdjustment; //Select necessary enumerator
ITCost.UnitCost = 75;
ITCost.Reference = "F2";
ITCost.Reference2 = "ref2";
ITCost.Description = "desc";
ITCost.Post();
```

Source: C Inventory Transactions page.

### 3. Credit Note
A credit note can be saved or processed like in Evolution. The example below shows creating a credit note, adding order detail lines for both inventory and GL accounts, and processing it:

```csharp
CreditNote CN = new CreditNote();
CN.Customer = new Customer("Customer1");
CN.InvoiceDate = DateTime.Now; // choose to set the invoice date or Order date etc
CN.InvoiceTo = CN.Customer.PostalAddress.Condense(); //Condense method can be used or you can specify the address as below
CN.DeliverTo = new Address("Physical Address 1", "Address 2", "Address 3",
"Address 4", "Address 5", "PC");
CN.Project = new Project("P1"); //Various CN properties like project can be set

OrderDetail OD = new OrderDetail();
CN.Detail.Add(OD);
//Various Order Detail properties can be added like warehouse, sales reps, user fields etc
OD.InventoryItem = new InventoryItem("ItemA"); //Use the inventoryItem constructor to specify an item
OD.Quantity = 10;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

OD = new OrderDetail();
CN.Detail.Add(OD);
OD.GLAccount = new GLAccount("Accounting Fees"); //Use the GLAccount Item constructor to specify an account
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 30;

CN.Process();
```

Source: C Credit Note page.

### 4. Return to Suppliers
A Return To Supplier (RTS) document can be saved or processed like in Evolution. The first example places an RTS and shows how to add order detail lines for inventory and GL accounts:

```csharp
ReturnToSupplier RTS = new ReturnToSupplier();
RTS.Supplier = new Supplier("SupplierSDK1");
RTS.InvoiceDate = DateTime.Now; // choose to set the invoice date or Order date etc
RTS.InvoiceTo = RTS.Supplier.PostalAddress.Condense(); //Condense method can be used or you can specify the address as below
RTS.DeliverTo = new Address("Physical Address 1", "Address 2", "Address 3",
"Address 4", "Address 5", "PC");
RTS.Project = new Project("P1"); //Various RTS properties like project can be set

OrderDetail OD = new OrderDetail();
RTS.Detail.Add(OD);
//Various Order Detail properties can be added like warehouse, sales reps, user fields etc
OD.InventoryItem = new InventoryItem("ItemA"); //Use the inventoryItem constructor to specify an item
OD.Quantity = 10;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

OD = new OrderDetail();
RTS.Detail.Add(OD);
OD.GLAccount = new GLAccount("Accounting Fees"); //Use the GLAccount Item constructor to specify an account
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 30;

RTS.Process();
```

Additional costs can be specified on an RTS. A `CostAllocation` object is created for each supplier cost, saved, added to the RTS, and then order detail lines specify the distribution or call `_Distribute()` to spread the cost evenly:

```csharp
ReturnToSupplier RTS = new ReturnToSupplier();
RTS.Supplier = new Supplier("SupplierSDK1");

//Specify additional costs on a Return To Supplier
CostAllocation costAlloc = new CostAllocation();
costAlloc.Supplier = new Supplier("Supplier1");
costAlloc.Reference = "ADDCost";
costAlloc.Description = "AddCost";
costAlloc.Amount = 200;
costAlloc.TaxRateID = 3;
costAlloc.Save(); // Save the additional cost lines
RTS.AdditionalCosts.Add(costAlloc); //Add the total additional costs to the RTS

OrderDetail OD = new OrderDetail();
RTS.Detail.Add(OD);
OD.InventoryItem = new InventoryItem("ItemA");
OD.TotalAdditionalCost = 100; // Specify additional costs per line or use distribute method
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

OD = new OrderDetail();
RTS.Detail.Add(OD);
OD.InventoryItem = new InventoryItem("ItemB");
OD.TotalAdditionalCost = 100;
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 30;

//RTS.AdditionalCosts._Distribute(); // Use this to distribute costs evenly
RTS.Process();
```

Source: C Return To Suppliers page.

### 5. Warehouse Transfer
Warehouse transfers are supported in the SDK for transferring stock between warehouses (IBT transfers before version 8 are not supported). To perform a warehouse transfer, create an instance of `WarehouseTransfer`, specify the inventory item, the `FromWarehouse`, `ToWarehouse`, quantity and references, and then call `Post()`:

```csharp
//Create an instance of the WarehouseTransfer Class
WarehouseTransfer WT = new WarehouseTransfer();
WT.Account = new InventoryItem("Itemw1"); //specify the item to transfer
WT.FromWarehouse = new Warehouse("w1"); //Specify the From warehouse
WT.ToWarehouse = new Warehouse("w2"); //Specify the To warehouse
WT.Quantity = 1; //Specify the quantity to transfer
WT.Reference = "ref1";
WT.Reference2 = "ref2";
//Post the warehouse transfer
WT.Post();
```

Source: C Warehouse Transfer page.

### 6. Warehouse Inter Branch Transfer (IBT)
From version 8.0 onwards, the SDK allows Warehouse Inter Branch Transfer (IBT). This requires enabling the feature in Evolution’s Warehouse defaults. A transfer is issued by creating a `WarehouseIBT` object, specifying the from/to warehouses, description and adding `WarehouseIBTLine` objects for each inventory item to transfer. Then call `IssueStock()`. To receive the stock, instantiate `WarehouseIBT` with the issue number, update `QuantityReceived` for each line and call `ReceiveStock()`.

```csharp
//Issue stock from one warehouse to another (stock will be in transit)
WarehouseIBT IBTIssue = new WarehouseIBT();
IBTIssue.WarehouseFrom = new Warehouse("W1"); //From which warehouse
IBTIssue.WarehouseTo = new Warehouse("W2"); //To which warehouse
IBTIssue.Description = "Test1des";

WarehouseIBTLine IBTIssueLine = new WarehouseIBTLine();
IBTIssueLine.InventoryItem = new InventoryItem("ItemwA");
IBTIssueLine.Description = "testline1";
IBTIssueLine.Reference = "Ref001";
IBTIssueLine.QuantityIssued = 5;
IBTIssue.Detail.Add(IBTIssueLine);
IBTIssue.IssueStock();

//Receive stock that is in transit
WarehouseIBT IBTReceive = new WarehouseIBT(IBTIssue.Number);
foreach (WarehouseIBTLine IBTReceiveLine in IBTReceive.Detail)
{
    IBTReceiveLine.QuantityReceived = 2;
}
IBTReceive.ReceiveStock();
```

Source: C Warehouse Inter Branch Transfer page.

## Order Entry

### 1. Purchase Orders and Purchase Order Invoices
The SDK allows processing supplier invoice documents via the `PurchaseOrder` class. Orders can be saved (`Save()`) or processed into supplier invoices via `Process()`. When a purchase order is placed, it only affects the quantities ordered and is not posted to accounting tables; once processed, the appropriate posting tables are populated (GL, supplier ledger, inventory). Orders can be partially processed or completed later.

#### Placing and partially processing a purchase order

```csharp
//Create a new instance of a PurchaseOrder class
PurchaseOrder order = new PurchaseOrder();
Supplier supp = new Supplier("Supplier1");
order.Supplier = supp; //Assign a value to the Supplier property

//Add document lines
order.Detail.Add("ItemA", 5, 10);
order.Save(); //Place the Order
string orderNo = order.OrderNo;
MessageBox.Show(orderNo);

//Partially process the order
order.Detail[0].ToProcess = 2;
order.Process();
```

#### Processing the entire outstanding quantity (complete the order)

```csharp
PurchaseOrder newOrder = new PurchaseOrder(orderNo);
newOrder.Complete(); //Completes the whole unprocessed order
```

The SDK provides several overloaded `Add` methods to suit different requirements. Each sales order must have an order number; when saving, either supply your own number or retrieve the generated one via `order.OrderNo`. Important: if `ToProcess` is set to 5 (equal to the quantity ordered) and you call `Process()`, the order is completed, a supplier invoice is created and the order becomes archived.

#### Purchase orders with GL and inventory lines and addresses

```csharp
PurchaseOrder PO = new PurchaseOrder();
PO.Supplier = new Supplier("SupplierSDK1");
PO.InvoiceDate = DateTime.Now; //Set invoice date or order date
PO.InvoiceTo = PO.Supplier.PostalAddress.Condense(); //Condense method can be used
PO.DeliverTo = new Address("Physical Address 1", "Address 2", "Address 3",
"Address 4", "Address 5", "PC");
PO.Project = new Project("P1"); //Various PO properties like project can be set

OrderDetail OD = new OrderDetail();
PO.Detail.Add(OD);
//Various Order Detail properties can be added like warehouse, sales reps, user fields etc
OD.InventoryItem = new InventoryItem("ItemA");
OD.Quantity = 10;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

OD = new OrderDetail();
PO.Detail.Add(OD);
OD.GLAccount = new GLAccount("Accounting Fees");
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 30;

PO.Process();
```

#### Receiving stock and processing the supplier invoice later (GRV / SINV split)
The SDK can receive stock while delaying the supplier invoice. Ensure `Evolution → Order Entry → Defaults → Purchase Orders` is set to Split GRV from SINV and configure the account types on the inventory SINV transaction type. First call `ProcessStock()` to receive the goods, then later call `Process()` on a `PurchaseOrder` object referencing the order and GRV numbers:

```csharp
PurchaseOrder PO = new PurchaseOrder();
PO.Supplier = new Supplier("SupplierSDK1");

OrderDetail OD = new OrderDetail();
PO.Detail.Add(OD);
OD.InventoryItem = new InventoryItem("ItemWA");
OD.Warehouse = new Warehouse("mstr");
OD.Quantity = 10;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

PO.ProcessStock(); // Process the GRV and receive stock while supplier invoice is unprocessed

//////////////////////////////////Process the unprocessed Supplier Invoice////////////////////////////////////
PurchaseOrder SINV = new PurchaseOrder("PO0023", "GRV0028"); //Specify the order number and GRV to get the unprocessed supplier invoice
foreach (OrderDetail NewOD in SINV.Detail)
{
    NewOD.ToProcess = NewOD.Outstanding; //process outstanding quantity or change price if needed
}
SINV.Process(); //Process the supplier invoice
```

#### Specifying additional costs on a purchase order
Additional costs can be allocated to a purchase order. Create a `CostAllocation` for each cost, save it, add it to the `AdditionalCosts` collection and allocate per line or distribute evenly:

```csharp
PurchaseOrder PO = new PurchaseOrder();
PO.Supplier = new Supplier("SupplierSDK1");

//Additional costs
CostAllocation costAlloc = new CostAllocation();
costAlloc.Supplier = new Supplier("Supplier1");
costAlloc.Reference = "ADDCost";
costAlloc.Description = "AddCost";
costAlloc.Amount = 200;
costAlloc.TaxRateID = 3;
costAlloc.Save(); // Save the additional cost lines
PO.AdditionalCosts.Add(costAlloc); //Add the total additional costs to the PO

OrderDetail OD = new OrderDetail();
PO.Detail.Add(OD);
OD.InventoryItem = new InventoryItem("ItemA");
OD.TotalAdditionalCost = 100; //Specify cost per line or remove and distribute evenly below
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

OD = new OrderDetail();
PO.Detail.Add(OD);
OD.InventoryItem = new InventoryItem("ItemB");
OD.TotalAdditionalCost = 100;
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 30;

//PO.AdditionalCosts._Distribute(); //Use this when not specifying costs per line
PO.Process();
```

The documentation notes that inventory documents can be populated in tax exclusive or tax inclusive mode via the `TaxMode` property on the order. When setting prices, set `TaxMode` first. For example:

```csharp
PurchaseOrder pord = new PurchaseOrder();
pord.Account = new Supplier("ABC009");
pord.TaxMode = TaxMode.Inclusive;
```

If you wish to supply your own invoice number when processing or completing a document, use the overloads of `Complete` or `Process` that accept a reference parameter; otherwise the system will generate a number.

Sources: C Purchase Orders page.

### 2. Sales Orders and Sales Order Invoices
The SDK processes invoice documents through the `SalesOrder` class. Sales orders can be saved or processed into invoices. When a sales order is placed, it affects quantities ordered but not posting tables. When processed, it posts to GL, customer ledger and inventory. Orders can be partially processed, quotations created and quantities reserved.

#### Placing and partially processing a sales order

```csharp
//Create a new instance of a SalesOrder class
SalesOrder order = new SalesOrder();
Customer cust = new Customer("Customer1");
order.Customer = cust; //Assign value to the customer property

//Add document lines
order.Detail.Add("ItemA", 5, 10);
order.Save(); //Place the order
string orderNo = order.OrderNo;
MessageBox.Show(orderNo);

//Partially process the order into an invoice
order.Detail[0].ToProcess = 2;
order.Process();
```

#### Completing the remaining quantity (completing the order)

```csharp
SalesOrder newOrder = new SalesOrder(orderNo);
newOrder.Complete(); //Complete the whole unprocessed order
```

As with purchase orders, the `Add` methods have various overloads. When saving, you can either supply your own order number or use the generated one. Setting `ToProcess` equal to the order quantity completes the order (creates an invoice) and archives it.

#### Sales orders with GL and inventory lines, default price lists and addresses

```csharp
SalesOrder SO = new SalesOrder();
SO.Customer = new Customer("CustomerSDK1");
SO.InvoiceDate = DateTime.Now; // set invoice or order date
SO.InvoiceTo = SO.Customer.PostalAddress.Condense(); //Use condense method or specify address
SO.DeliverTo = new Address("Physical Address 1", "Address 2", "Address 3",
"Address 4", "Address 5", "PC");
SO.Project = new Project("P1"); //Various SO properties like project can be set

OrderDetail OD = new OrderDetail();
SO.Detail.Add(OD);
//Various Order Detail properties can be added like warehouse, sales reps, user fields etc
OD.InventoryItem = new InventoryItem("ItemA");
OD.Quantity = 10;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = OD.InventoryItem.SellingPrices[SO.Customer.DefaultPriceList].PriceIncl; //Use customer default price list

OD = new OrderDetail();
SO.Detail.Add(OD);
OD.GLAccount = new GLAccount("Accounting Fees");
OD.Quantity = 1;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 30;

SO.Process();
```

#### Sales order quotations
The SDK can create and process Sales Order Quotations. A quotation is created using `SalesOrderQuotation`, details are added, and then `Save()` is used. To turn a quote into an invoice, call `Process()` on an existing quotation object.

```csharp
SalesOrderQuotation SOQ = new SalesOrderQuotation();
SOQ.Customer = new Customer("Cust1");

OrderDetail OD = new OrderDetail();
SOQ.Detail.Add(OD);
//Various Order Detail properties can be added like warehouse, sales reps, user fields etc
OD.InventoryItem = new InventoryItem("ItemA");
OD.Quantity = 10;
OD.ToProcess = OD.Quantity;
OD.UnitSellingPrice = 20;

SOQ.Save(); //Save the quote; you can later process it into an invoice
```

#### Reserving quantity on a sales order
If `Order Entry Defaults → Sales Orders` is set to reserve quantities, a sales order can reserve stock by saving (not processing) the order and setting the `Reserved` property on each line. Example:

```csharp
SalesOrder SO = new SalesOrder();
SO.Customer = new Customer("Cust1");

OrderDetail OD = new OrderDetail();
SO.Detail.Add(OD);
//Add properties as required
OD.InventoryItem = new InventoryItem("ItemA");
OD.Quantity = 5;
OD.Reserved = 5; //Specify quantity to reserve
OD.UnitSellingPrice = 20;

SO.Process(); //To reserve quantity the order must be saved, not processed
```

#### Processing an existing sales order
The SDK can process an existing sales order by specifying the order number and then iterating through the detail lines, setting `ToProcess` equal to the outstanding quantity for each line and calling `Process()`:

```csharp
SalesOrder SO = new SalesOrder("SO00023"); //Specify the order number to process
foreach (OrderDetail NewOD in SO.Detail)
{
    NewOD.ToProcess = NewOD.Outstanding; //Process outstanding quantity for each line; you can change the price if needed
}
SO.Process(); //Process the SalesOrder
```

#### Tax mode and specifying invoice numbers
As with purchase orders, inventory documents can be set to tax inclusive or exclusive via the `TaxMode` property. Set `TaxMode` before setting the unit price:

```csharp
SalesOrder sord = new SalesOrder();
sord.Account = new Customer("ABC009");
sord.TaxMode = TaxMode.Inclusive;
```

To supply your own invoice number when processing or completing a sales order, use the overloads of `Complete` or `Process` accepting a reference parameter; otherwise the SDK generates a number.

Sources: C Sales Orders page.

## Contact Management Incidents
The SDK allows creating incidents and posting actions to existing incidents. An `Incident` is created by setting properties such as customer, outline, incident type, priority, due date and category. Actions are created via `NewAction()` and posted using `inc.Post(action)`. You can also link documents to an incident.

```csharp
{
    //Create an instance of the Incident class
    Incident inc = new Incident(); //specify an existing incident number here if using an existing incident
    inc.Customer = new Customer("CASH");
    inc.Outline = "Test Incident";
    inc.IncidentType = new IncidentType("Undefined");
    inc.Priority = new Priority("High");
    inc.DueBy = DateTime.Now.AddHours(24);
    //inc.Contract = new Contract(1); //Contract templates have only one overload that is the contract id
    inc.Category = new IncidentCategory("category1");

    IncidentLogEntry action = inc.NewAction();
    action.Agent = new Agent("Admin");
    action.NewAgent = new Agent("Admin");
    action.Resolution = "Content";

    //Log and post the incident action
    inc.Post(action);

    //Linking of documents to an incident can be done as follows if required
    //Remember to specify document storage path in Contact management defaults
    {
        string DocDescr = "TestDoc";
        if (Document.FindByDescription(DocDescr) == -1)
        {
            Document doc = new Document();
            doc.Name = "TestDoc";
            doc.Description = DocDescr;
            doc.Save(@"C:\\Evofiles\\test.txt");
        }
    }

    Document docexist = new Document(DocDescr);
    docexist._CreateLink(inc);
}
```

Source: C Contact Management Incidents page.

## Job Costing
Job cards can be created as active or as a quote in the SDK and saved, but they cannot be processed or completed via the SDK. The example below shows creating a job card, adding various lines (inventory, return, supplier, financial) and saving the job card:

```csharp
//Create a new instance of the JobCard class
JobCard card = new JobCard(); //An existing job can be specified
card.Account = new Customer("Cash");
card.Description = "SDK Job Card ";

//Make the Job Active
card.Status = JobCard.JobStatus.Active;

// Add an Active Inventory Line to the Job Card
JobDetail det = new JobDetail();
det.TransactionCode = new JobTransactionCode(JobDetail.TransactionSource.Inventory, "ST");
det.Status = JobCard.JobStatus.Active;
det.Account = new InventoryItem("ItemA");
det.Warehouse = new Warehouse("CPT");
det.Quantity = 3;
card.Detail.Add(det);

// Add a Return Line to the Job Card
det = new JobDetail();
det.TransactionCode = new JobTransactionCode(JobDetail.TransactionSource.Inventory, "ST");
det.Status = JobCard.JobStatus.Active;
det.Account = new InventoryItem("ItemA");
det.Warehouse = new Warehouse("CPT");
det.Quantity = -2;
card.Detail.Add(det);

//Add a Supplier Line to the Job Card
det = new JobDetail();
det.TransactionCode = new JobTransactionCode(JobDetail.TransactionSource.Payables, "SC");
det.Status = JobCard.JobStatus.Active;
det.Account = new Supplier("Supplier1");
det.UnitCostPrice = 60;
det.UnitSellingPrice = 200;
card.Detail.Add(det);

//Add a financial Line to the Job Card
det = new JobDetail();
det.TransactionCode = new JobTransactionCode(JobDetail.TransactionSource.Ledger, "FL");
det.Status = JobCard.JobStatus.Active;
det.Account = new GLAccount("Accounting fees");
det.UnitCostPrice = 50;
det.UnitSellingPrice = 200;
card.Detail.Add(det);

// Save the Job Card – job cards cannot be completed or processed
card.Save();
```

Source: C Job Costing page.

## Additional Functionality
This section lists miscellaneous features available in the SDK.

1. **User authentication**

    ```csharp
    bool valid = Agent.Authenticate(AgentName.Text, Password.Text);
    MessageBox.Show(valid.ToString());
    ```

2. **Posting transactions on behalf of a user**

    ```csharp
    DatabaseContext.CurrentAgent = new Agent("User2");
    ```

3. **Setting branch context (online branch accounting)**

    ```csharp
    DatabaseContext.SetBranchContext(1);
    ```

4. **User defined fields (UDF) and user fields**
User defined fields on orders or other entities can be updated via the `UserFields` collection. For example, adding a user field to a sales order:

    ```csharp
    SalesOrder SO = new SalesOrder("SO00023"); //Specify the order number to process
    SO.UserFields["ucIITestSO"] = "this is a user field";
    ```

    User fields can also update existing fields when direct properties are not available; note that field names are case sensitive:

    ```csharp
    Customer cust = new Customer();
    cust.Code = "Test1";
    cust.Description = "test1";
    cust.UserFields["BFOpenType"] = 1; //existing field BFOpenType must match case
    cust.Save();
    ```

5. **Listing unprocessed and processed orders**

    ```csharp
    dataGridView1.DataSource = SalesOrder.List(" Account = 'Cash'");
    ```

6. **Listing price lists per stock item**

    ```csharp
    DataTable PriceList = SellingPrice.ListByStockItemID(1002);
    ```

7. **Working with multiple Evolution databases**
To switch between Evolution company databases, use `DatabaseContext.CreateConnection`. Each call closes the active connection and opens the new one. Note that switching connections rolls back any active transactions and invalidates in-memory records from the previous connection. To minimise switching overhead, group transactions by target database and switch as infrequently as possible. Remember that any open transaction will be rolled back when switching.

8. **Preventing duplicate transactions**
The SDK does not prevent duplicate transactions. You must implement your own duplication checks. A sample function uses `CustomerTransaction.Find()` to check if a transaction with the same reference, module ID and account ID already exists:

    ```csharp
    private bool isTransactionPosted(CustomerTransaction transaction)
    {
        string criteria = string.Format(@"Reference = '{0}' and Id = '{1}' and AccountLink = {2}",
            transaction.Reference, transaction.ModID.ToString(),
            transaction.AccountID);
        return CustomerTransaction.Find(criteria) != -1;
        // ..or just raise an exception
    }
    ```

9. **Transaction scopes and batching**
Earlier SDK versions required explicit transaction scopes for every operation. Newer versions implicitly create a transaction scope if one does not exist, but you may still manage transactions manually when processing many accounting transactions or when posting GL transactions (since multiple GL transactions must balance). Use `BeginTran()`, `CommitTran()` and `RollbackTran()` to manage the scope. It is advisable to call `StartNewBatch()` after each transaction when posting many transactions; if you wish to group them into a single batch, omit `StartNewBatch()`. The following pattern illustrates manual transaction control:

    ```csharp
    try
    {
        BeginTran();
        [loop]
        {
            [post transaction]
            StartNewBatch();
        }
        CommitTran();
    }
    catch
    {
        RollbackTran();
    }
    ```

10. **Document printing and reporting**
The SDK does not provide printing components. The Evolution printing component is specific to the application and not available in the SDK. You must generate documents for printing in your own project. You can create print groups and link documents together for later batch printing inside Evolution.

Source: C Additional Functionality page.

## Conclusion
This document reproduces the complete C# transaction user-guide from the Sage Evolution SDK. It covers creating and posting transactions across Accounts Payable, Accounts Receivable, General Ledger, Inventory, Order Entry, Contact Management, Job Costing and various additional functions. Each section includes the full code examples and instructions, with references to the official documentation.
