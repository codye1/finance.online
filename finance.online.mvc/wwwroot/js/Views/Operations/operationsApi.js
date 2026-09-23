const operationsApi = {
    createOperation: function (operationData, organizationId) {
        return $.ajax({
            url: '/organizations/' + organizationId + '/operations?view=operations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(operationData)
        });
    },
    deleteOperation: function (operationId) {
        return $.ajax({
            url: '/operations/' + operationId,
            method: 'DELETE'
        });
    }
};

export default operationsApi;