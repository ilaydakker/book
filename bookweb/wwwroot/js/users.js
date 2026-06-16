var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/user/getall' },
        "columns": [
            { data: 'name', "width": "18%" },
            { data: 'email', "width": "22%" },
            { data: 'phoneNumber', "width": "12%" },
            {
                data: 'role',
                "width": "12%",
                "render": function (data) {
                    var badgeClass = 'bg-secondary';
                    if (data === 'Admin') badgeClass = 'bg-danger';
                    else if (data === 'Editor') badgeClass = 'bg-warning text-dark';
                    else if (data === 'Customer') badgeClass = 'bg-success';
                    return `<span class="badge ${badgeClass}">${data}</span>`;
                }
            }
        ]
    });
}

function lockUnlock(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "Do you want to change this user's lock status?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#065f46',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, proceed!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/admin/user/lockunlock',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ id: id }),
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}
