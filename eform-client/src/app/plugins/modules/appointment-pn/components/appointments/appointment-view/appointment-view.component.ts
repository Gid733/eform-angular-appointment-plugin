import {Component, EventEmitter, OnInit, Output, ViewChild} from '@angular/core';
import {AppointmentModel} from '../../../models';
import {AppointmentPnCalendarService} from '../../../services';
import * as moment from 'moment';

@Component({
  selector: 'app-appointment-view',
  templateUrl: './appointment-view.component.html',
  styleUrls: ['./appointment-view.component.scss']
})
export class AppointmentViewComponent implements OnInit {
  @ViewChild('frame') frame;
  @Output() appointmentSaved: EventEmitter<void> = new EventEmitter<void>();
  spinnerStatus = false;
  selectedModel: AppointmentModel = new AppointmentModel();

  constructor(private appointmentPnCalendarService: AppointmentPnCalendarService) {
  }

  ngOnInit() {
  }

  show(model?: AppointmentModel) {
    if (model) {
      this.spinnerStatus = true;
      this.appointmentPnCalendarService.getAppointment(model.id).subscribe((data) => {
        if (data && data.success) {
          this.selectedModel = data.model;
          this.selectedModel.startAt = moment(this.selectedModel.startAt);
          this.selectedModel.expireAt = moment(this.selectedModel.expireAt);
        }
        this.frame.show();
        this.spinnerStatus = false;
      });
    } else {
      this.frame.show();
    }
  }
}
