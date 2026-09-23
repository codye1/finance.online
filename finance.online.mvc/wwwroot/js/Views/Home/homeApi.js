const homeApi = {
    createOperation: function (operationData, organizationId) {
        return $.ajax({
            url: '/organizations/' + organizationId + '/operations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(operationData)
        });
    },
    createOrganization: function (organizationData) {
        return $.ajax({
            url: '/organizations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(organizationData)
        });
    }
};

export default homeApi;
