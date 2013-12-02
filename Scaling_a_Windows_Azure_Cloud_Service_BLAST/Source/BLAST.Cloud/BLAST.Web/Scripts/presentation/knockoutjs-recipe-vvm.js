ko.extenders.required = function (target, overrideMessage) {
    target.hasError = ko.observable();
    target.validationMessage = ko.observable();
    function validate(newValue) {
        target.hasError(newValue ? false : true);
        target.validationMessage(newValue ? "" : overrideMessage || "This field is required");
    }
    validate(target());
    target.subscribe(validate);
    return target;
}
ko.extenders.email = function (target, overrideMessage) {
    target.hasError = ko.observable();
    target.validationMessage = ko.observable();
    function validate(newValue) {
        var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
        var result = re.test(newValue);
        target.hasError(result ? false : true);
        target.validationMessage(result ? "" : overrideMessage || "This field needs to be a valid email");
    }
    validate(target());
    target.subscribe(validate);
    return target;
}