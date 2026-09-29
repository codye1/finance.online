const settingsApi = {
    deleteOrganization: function (organizationId) {
        return $.ajax({
            url: '/settings/delete-organization',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ organizationId: organizationId })
        });
    },
    inviteMember: function (memberData, organizationId) {
        return $.ajax({
            url: '/settings/invite-member',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                organizationId: organizationId,
                email: memberData.email,
                role: memberData.role
            })
        });
    },
    removeMember: function (userId, organizationId) {
        return $.ajax({
            url: '/settings/remove-member',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ organizationId: organizationId, userId: userId })
        });
    },
    createOrganization: function (organizationData) {
        return $.ajax({
            url: '/organizations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(organizationData)
        });
    },
};

export default settingsApi;