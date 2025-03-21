import React, { useEffect, useState } from 'react';
import { Trans, useTranslation } from 'react-i18next';
import { Input, Radio, Select } from '@alifd/next';
import { useUpdateEffect } from 'react-use';
import type { IVariable } from '../../Variables';
import type { ITextProcessorValue } from '../Processors/TextProcessor';
import TextProcessor from '../Processors/TextProcessor';
import { BmMultipleValueProcessorFilterBy, BmMultipleValueProcessorOperation } from '@/sdk/constants';

interface IProps {
  variables: IVariable[];
  getCandidates: () => Promise<{ label: string; value: number }[]>;
  onChange?: (value: IMultiValueProcessorValue) => any;
  value?: IMultiValueProcessorValue;
}

// enum Operation {
//   Add = 1,
//   Remove,
//   SetWithFixedValue,
//   Modify,
// }

// enum FilterBy {
//   All = 1,
//   Containing = 2,
//   Matching,
// }

export interface IMultiValueProcessorValue {
  operation?: BmMultipleValueProcessorOperation;
  selectedKeys?: number[];
  newData?: string[];
  filterBy?: BmMultipleValueProcessorFilterBy;
  find?: string;
  textProcessorValue?: ITextProcessorValue;
}

const Editor = ({
                  variables: propsVariables,
                  getCandidates,
                  onChange,
                  value: propsValue,
                }: IProps) => {
  const { t } = useTranslation();
  const [variables, setVariables] = useState<IVariable[]>(propsVariables || []);
  const [value, setValue] = useState<IMultiValueProcessorValue>(propsValue || {});

  const [candidates, setCandidates] = useState<{ label: string; value: number }[]>([]);
  const [newData, setNewData] = useState<string[]>([]);

  const operationDataSource = Object.keys(BmMultipleValueProcessorOperation).filter(k => Number.isNaN(parseInt(k, 10))).map(x => ({
    label: t(x),
    value: BmMultipleValueProcessorOperation[x],
  }));

  const filterByDataSource = Object.keys(BmMultipleValueProcessorFilterBy).filter(k => Number.isNaN(parseInt(k, 10))).map(x => ({
    label: t(x),
    value: BmMultipleValueProcessorFilterBy[x],
  }));

  // console.log(propsValue);

  useEffect(() => {
    getCandidates().then(r => setCandidates(r));
  }, []);

  useUpdateEffect(() => {
    if (onChange) {
      onChange(value);
    }
  }, [value]);

  const changeValue = (patches: Record<any, any>) => {
    setValue({
      ...value,
      ...patches,
    });
  };

  const renderValueComp = () => {
    const componentsData: { label: string; comp: any }[] = [];
    console.log(value, 5555);
    switch (value.operation) {
      case BmMultipleValueProcessorOperation.Add:
      case BmMultipleValueProcessorOperation.SetWithFixedValue:
        componentsData.push({
          label: t('Candidate(s)'),
          comp: (
            <Select
              dataSource={candidates}
              mode={'multiple'}
              showSearch
              autoWidth
              style={{ width: '90%' }}
              onChange={selectedKeys => changeValue({ selectedKeys })}
              value={value.selectedKeys}
            />
          ),
        });
        componentsData.push({
          label: t('New data'),
          comp: (
            <Select
              dataSource={newData}
              mode={'tag'}
              autoWidth
              style={{ width: '90%' }}
              onChange={newData => changeValue({ newData })}
              value={value.newData}
            />
          ),
        });
        break;
      case BmMultipleValueProcessorOperation.Remove:
      case BmMultipleValueProcessorOperation.Modify:
        componentsData.push({
          label: t('Filter'),
          comp: (
            <Radio.Group
              value={value.filterBy}
              dataSource={filterByDataSource}
              onChange={filterBy => {
                const changes: any = { filterBy };
                if (filterBy == BmMultipleValueProcessorFilterBy.All) {
                  changes.find = undefined;
                }
                changeValue(changes);
              }}
            />
          ),
        });
        if (value.filterBy == BmMultipleValueProcessorFilterBy.Matching || value.filterBy == BmMultipleValueProcessorFilterBy.Containing) {
          componentsData.push({
            label: t('Value'),
            comp: (
              <Input
                value={value.find}
                onChange={find => changeValue({ find })}
              />
            ),
          });
        }
        break;
    }
    const components = componentsData.map((c, i) => {
      return (
        <div className="block" key={i}>
          <div className="label">{c.label}</div>
          <div className="value">{c.comp}</div>
        </div>
      );
    });

    if (value.operation == BmMultipleValueProcessorOperation.Modify) {
      components.push(
        <TextProcessor.Editor
          value={value.textProcessorValue}
          variables={variables}
          key={componentsData.length}
          onChange={textProcessorValue => changeValue({ textProcessorValue })}
        />,
      );
    }
    return components;
  };

  return (
    <>
      <div className="block">
        <div className={'label'}>
          {t('Operation')}
        </div>
        <div className="value">
          <Select
            style={{ width: 300 }}
            dataSource={operationDataSource}
            value={value.operation}
            onChange={v => setValue({
              operation: v,
            })}
          />
        </div>
      </div>
      {renderValueComp()}
    </>
  );
};

const Demonstrator = ({
                        value,
                        getDataSource,
                      }: { value: IMultiValueProcessorValue; getDataSource: (keys: any[]) => Promise<string[]> }) => {
  const { t } = useTranslation();
  const [valueTexts, setValueTexts] = useState<string[]>([]);

  useEffect(() => {
    getDataSource(value.selectedKeys!).then(r => {
      // console.log(r, value.selectedKeys);
      setValueTexts(r.concat(value.newData || []));
    });
  }, [getDataSource, value]);

  switch (value.operation) {
    case BmMultipleValueProcessorOperation.Remove:
      return (
        <>
          <div className="primary">{t('Remove')}</div>
          <Trans
            i18nKey={'BulkModification.Processor.Demonstrator.Operation.Filter.FilteredData'}
            values={{
              find: value.find,
            }}
          >
            {/* Data filtered by xxx */}
            {t(BmMultipleValueProcessorFilterBy[value.filterBy!])}
            {value.find != undefined && (
              <div className="secondary">{value.find}</div>
            )}
          </Trans>
        </>
      );
    case BmMultipleValueProcessorOperation.Add:
      return (
        <>
          <div className="primary">{t('Add')}</div>
          {valueTexts.map((t, i) => {
            return (
              <React.Fragment key={i}>
                <div className="secondary">{t}</div>
                {i < valueTexts.length - 1 && ','}
              </React.Fragment>
            );
          })}
        </>
      );
    case BmMultipleValueProcessorOperation.SetWithFixedValue:
      return (
        <>
          <Trans
            i18nKey={'BulkModification.Processor.Demonstrator.Operation.SetDirectly'}
          >
            <div className="primary" />
            with fixed value
          </Trans>
          {valueTexts.map((t, i) => {
            return (
              <React.Fragment key={i}>
                <div className="secondary">{t}</div>
                {i < valueTexts.length - 1 && ','}
              </React.Fragment>
            );
          })}
        </>
      );
    case BmMultipleValueProcessorOperation.Modify:
      return (
        <>
          <Trans
            i18nKey={'BulkModification.Processor.Demonstrator.Operation.FilterThenModifyWithTextProcessor'}
            values={{
              filterBy: t(BmMultipleValueProcessorFilterBy[value.filterBy!]),
              find: value.find,
            }}
          >
            <div className="primary">
              {t(BmMultipleValueProcessorFilterBy[value.filterBy!])}
            </div>
            {(value.find == undefined || value.find.length == 0) ? (<></>) : (
              <div className="secondary">{value.find}</div>
            )}

            <TextProcessor.Demonstrator value={value.textProcessorValue!} />
          </Trans>
        </>
      );
    default:
      return (
        <>
          {t('Unsupported value')}
        </>
      );
  }
};

export default class MultiValueProcessor {
  static Editor = Editor;
  static Demonstrator = Demonstrator;
}
