Office.initialize = function (reason) {
    $(document).ready(function () {
        $('#healthSummary').click(function () {
            // call the health api and then populate a table in the current document with a health summary
            var serviceURL = '/Home/HealthSummary';

            $.ajax({
                type: "GET",
                url: serviceURL,
                dataType: "json",
                success: successFunc,
                error: errorFunc
            });

            function insertDataAsTable(data) {
                if (Office.context.document.setSelectedDataAsync) {
                    var summaryTab = new Office.TableData();
                    summaryTab.headers = ["Steps Taken", "Total Calories", "Av. Heart Rate", "Total Distance"];
                    for (var i = 0; i < data.itemCount; i++) {
                        var summary = data.summaries[i];
                        summaryTab.rows.push([
                            summary.stepsTaken ? summary.stepsTaken.toString() : '-',
                            summary.caloriesBurnedSummary.totalCalories ?
                                summary.caloriesBurnedSummary.totalCalories.toString() : '-',
                            summary.heartRateSummary.averageHeartRate ?
                                summary.heartRateSummary.averageHeartRate.toString() : '-',
                            summary.distanceSummary.totalDistance ?
                                summary.distanceSummary.totalDistance.toString() : '-'
                            ]);
                    }

                    Office.context.document.setSelectedDataAsync(summaryTab,
                        { coercionType: "table", asyncContext: this/*, tableOptions: {headerRow:false}*/ },
                        function (result) {
                            if (result.status === Office.AsyncResultStatus.Failed) {
                                app.showNotification("There's a problem!",
                                    "unable to add table");
                            } else {
                            }
                        });
                } else {
                    app.showNotification('Content insertion not supported');
                }
            }

            function insertDataAsMatrix(data) {
                // turn this data into tabular data and insert into document
                if (Office.context.document.setSelectedDataAsync) {
                    // generate a summary tab from the data passed in.
                    var summaryTab = [[]];
                    for (var i = 0; i < data.itemCount; i++) {
                        var summary = data.summaries[i];
                        summaryTab[i] = [
                            summary.stepsTaken ? summary.stepsTaken.toString() : '-',
                            summary.caloriesBurnedSummary.totalCalories ?
                                summary.caloriesBurnedSummary.totalCalories.toString() : '-',
                            summary.heartRateSummary.averageHeartRate ?
                                summary.heartRateSummary.averageHeartRate.toString() : '-',
                            summary.distanceSummary.totalDistance ?
                                summary.distanceSummary.totalDistance.toString() : '-'];
                    }

                    Office.context.document.setSelectedDataAsync(summaryTab,
                        { coercionType: "matrix", asyncContext: this/*, tableOptions: {headerRow:false}*/ },
                        function (result) {
                            if (result.status === Office.AsyncResultStatus.Failed) {
                                app.showNotification("There's a problem!",
                                    "unable to add table" + result.error.message);
                            } else {
                            }
                        });
                } else {
                    app.showNotification('Content insertion not supported');
                }
            }

            function successFunc(data, status) {
                insertDataAsTable(data);
            }

            function errorFunc() {
                app.showNotification("There's a problem!", "Error retrieving data from health API"); 
            }

            var doc = Office.context.document;

            // want to make AJAX call to get the health summary and then insert into office document..
        });
        app.initialize();
    });
};
