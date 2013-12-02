function testVM(id, name, inputFile, state, hash, outputFile, message) {
    var self = this;
    var deferred = $.Deferred();
    self.defaultName = function (name) {
        if (!name || name=='')
        {
            var d = new Date();
            return 'TEST ' + d.getFullYear() + '-' + d.getMonth() + '-' + d.getDate() + '-'
            + d.getHours() + '-' + d.getMinutes() + '-' + d.getSeconds() + '-' + d.getMilliseconds();
        }
        else
            return name;
    }
    self.id = ko.observable(id);
    self.name = ko.observable(self.defaultName(name)).extend({ required: "Task name can't be null." });;
    self.inputFile = ko.observable(inputFile);
    self.inputFiles = ko.observable('');
    self.state = ko.observable(state);
    self.hash = ko.observable(hash);
    self.outputFile = ko.observable(outputFile);
    self.message = ko.observable(message);
    self.hasError = ko.observable(false);
    self.validationMessage = ko.observable('Please select at least one input file.');
    self.hasServerError = ko.observable(false);
    self.serverValidationMessage = ko.observable('');
    self.validate = function () {
        self.hasError(!(self.inputFile() != '' || self.inputFiles() != ''));
        return !self.hasError();
    }
    self.create = function () {
        $.ajax({
            type: 'POST',
            url: '/api/SearchTaskManager',
            data: {
                Id: self.id(),
                Name: self.name(),
                InputFile: self.inputFile() ,
                InputFiles: self.inputFiles()
            },
            success: function (data, status, jqXHR) {
                self.hasServerError(false);
                deferred.resolve(status);
            },
            error: function (jqXHR, status, error) {
                self.hasServerError(true);
                self.serverValidationMessage(error);
                deferred.reject(status);
            }
        });
        return deferred;
    };
    self.inputs = ko.observableArray([]);
    self.refresh = function () {
        var files = new Array();
        $.getJSON('/api/InputFileManager', function (json) {
            self.inputs.removeAll();
            files.push(''); //add an empty string for no selection
            $.each(json, function (key, val) {
                files.push(val);
            });
            self.inputs(files);
        });
    }
    self.retryTask = function (task) {
        $.ajax({
            type: 'PUT',
            url: '/api/SearchTaskManager/' + task.id(),
            success: function (data, status, jqXHR) { self.refresh(); },
            error: function (jqXHR, status, error) { self.refresh(); }
        });
    }
}
function testsVM() {
    var self = this;
    var deferred = $.Deferred();
    self.tests = ko.observableArray([]);
    self.refresh = function () {
        $.getJSON('/api/SearchTaskManager', function (json) {
            self.tests.removeAll();
            $.each(json, function (key, val) {
                self.tests.push(new testVM(val.Id, val.Name, val.InputFile, val.State, val.Hash, val.OutFile, val.LastMessage));
            });
        });
    };
    self.updateTask = function (taskId, state, hash, output, message) {
        var match = ko.utils.arrayFirst(self.tests(), function (item) {
            return taskId === item.id();
        });

        if (match) {
            match.state(state);
            match.hash(hash);
            match.outputFile(output);
            match.message(message);
        }
        else
            self.refresh(); // probably someone else has scheduled a job, pick it up.
    }
    self.deleteTask = function (task) {
        $.ajax({
            type: 'DELETE',
            url: '/api/SearchTaskManager/' + task.id(),
            success: function (data, status, jqXHR) { self.refresh(); },
            error: function (jqXHR, status, error) { self.refresh(); }
        });
    }
    self.deleteAllCompleted = function () {
        $.ajax({
            type: 'DELETE',
            url: '/api/SearchTaskManager/OK',
            success: function (data, status, jqXHR) { self.refresh(); },
            error: function (jqXHR, status, error) { self.refresh(); }
        });
    }
    self.deleteAllQueued = function () {
        $.ajax({
            type: 'DELETE',
            url: '/api/SearchTaskManager/QUEUED',
            success: function (data, status, jqXHR) { self.refresh(); },
            error: function (jqXHR, status, error) { self.refresh(); }
        });
    }
}